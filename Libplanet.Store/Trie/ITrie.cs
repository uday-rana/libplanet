using System.Collections.Generic;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Common;
using Libplanet.Store.Trie.Nodes;

namespace Libplanet.Store.Trie
{
    /// <summary>
    /// An interface for <see href="https://en.wikipedia.org/wiki/Merkle_tree">Merkle Tree</see>.
    /// </summary>
    /// <seealso cref="MerkleTrie"/>
    public interface ITrie
    {
        /// <summary>
        /// The root of the <see cref="ITrie"/>.  This is <see langword="null"/> if and only if
        /// the <see cref="ITrie"/> is empty.  That is, this is never a "hashed node" of a
        /// <see langword="null"/> root.
        /// </summary>
        /// <seealso cref="Hash"/>
        INode? Root { get; }

        /// <summary>
        /// The state root hash of the trie.
        /// </summary>
        /// <remarks>
        /// If <see cref="Root"/> is <see langword="null"/>, this still gives a unique
        /// <see cref="HashDigest{SHA256}"/> value corresponding to <see langword="null"/>
        /// that is <em>never recorded</em>.
        /// </remarks>
        /// <seealso cref="Root"/>
        HashDigest<SHA256> Hash { get; }

        /// <summary>
        /// Whether values are stored at hashed location or not.  When referring to paths in
        /// <see cref="ITrie"/> context, we have the following:
        /// <list type="bullet">
        ///     <item><description>
        ///         Original path: A path that a user wishes to store a value at.
        ///         This is what is used to access values via <see cref="Set"/> and
        ///         <see cref="Get(KeyBytes)"/>.
        ///     </description></item>
        ///     <item><description>
        ///         Secure path: A hashed path of an original path.  If <see cref="Secure"/>
        ///         is <see langword="true"/>, the actual location of a value accessed
        ///         is obfuscated internally.
        ///     </description></item>
        ///     <item><description>
        ///         Real path: A path that may be an original path or a secure path
        ///         depending on the value of <see cref="Secure"/>.
        ///     </description></item>
        /// </list>
        /// </summary>
        /// <seealso cref="Set"/>
        /// <seealso cref="Get(KeyBytes)"/>
        /// <seealso cref="Get(IReadOnlyList{KeyBytes})"/>
        bool Secure { get; }

        /// <summary>
        /// Whether <see cref="Root"/> is recorded in the store.
        /// </summary>
        /// <remarks>A <see cref="Root"/> that is <see langword="null"/> is always considered
        /// as recorded.
        /// </remarks>
        bool Recorded { get; }

        /// <summary>
        /// Stores the <paramref name="value"/> to the
        /// node corresponding to given <paramref name="key"/> <em>in memory</em>.
        /// </summary>
        /// <param name="key">The unique key to associate with the <paramref name="value"/>.</param>
        /// <param name="value">The value to store.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when the given
        /// <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <returns>Returns new updated <see cref="ITrie"/>.</returns>
        /// <remarks>
        /// This <em>should not</em> actually write anything to storage.
        /// </remarks>
        /// <seealso cref="IStateStore.Commit"/>
        ITrie Set(in KeyBytes key, IValue value);

        /// <summary>
        /// Gets the values stored with <paramref name="key"/> in <see cref="Set"/>.
        /// </summary>
        /// <param name="key">The key used in <see cref="Set"/> to store a value.</param>
        /// <returns>The value associated to the specified <paramref name="key"/>.  Absent
        /// value is represented as <see langword="null"/>.</returns>
        public IValue? Get(KeyBytes key);

        /// <summary>
        /// Gets the values stored with <paramref name="keys"/> in <see cref="Set"/>.
        /// </summary>
        /// <param name="keys">The keys used in <see cref="Set"/> to store a value.</param>
        /// <returns>The values associated to the specified <paramref name="keys"/>.  The associated
        /// values are ordered in the same way to the corresponding <paramref name="keys"/>.  Absent
        /// values are represented as <see langword="null"/>.</returns>
        IReadOnlyList<IValue?> Get(IReadOnlyList<KeyBytes> keys);

        /// <summary>
        /// Gets the first node encountered at <paramref name="nibbles"/> when traversing down
        /// from <see cref="Root"/>.
        /// </summary>
        /// <param name="nibbles">The path to check.  This must be the <em>real</em> path,
        /// which depends on <see cref="Secure"/>.</param>
        /// <returns>A node at <paramref name="nibbles"/>, if any.
        /// Otherwise <see langword="null"/>.</returns>
        /// <exception cref="InvalidTrieNodeException">Thrown when an unknown type
        /// of <see cref="INode"/> is encountered while traversing to the given path.</exception>
        /// <remarks>
        /// <para>
        /// There may be more than one <see cref="INode"/> at <paramref name="nibbles"/>.
        /// For instance, a <see cref="FullNode"/>, a <see cref="ValueNode"/> as the value of the
        /// aforementioned <see cref="FullNode"/>, and up to two additional <see cref="HashNode"/>s
        /// is possible.
        /// </para>
        /// <para>
        /// As such, for two equivalent <see cref="ITrie"/>s, <see cref="Trie"/>s that
        /// <em>would have the same committed <see cref="Hash"/>es</em>, this may return different
        /// types of <see cref="INode"/> depending on the actual underlying "structure".
        /// However, returned <see cref="INode"/>s for such <see cref="ITrie"/>s are
        /// equivalent as sub-<see cref="ITrie"/>s.
        /// </para>
        /// </remarks>
        /// <seealso cref="Secure"/>
        INode? GetNode(Nibbles nibbles);

        /// <summary>
        /// Iterates and every stored <see cref="IValue"/> along with its respective
        /// <em>real</em> path in
        /// <see cref="KeyBytes"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of all <see cref="IValue"/>s
        /// in no particular order.</returns>
        /// <remarks>
        /// This is a very heavy operation.
        /// </remarks>
        /// <seealso cref="Secure"/>
        IEnumerable<(KeyBytes Path, IValue Value)> IterateValues();

        /// <summary>
        /// Iterates and every stored <see cref="INode"/> along with its respective
        /// <em>real</em> path in
        /// <see cref="Nibbles"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of all <see cref="INode"/>s
        /// in no particular order.</returns>
        /// <remarks>
        /// This is a very heavy operation.
        /// </remarks>
        /// <seealso cref="Secure"/>
        IEnumerable<(Nibbles Path, INode Node)> IterateNodes();
    }
}
