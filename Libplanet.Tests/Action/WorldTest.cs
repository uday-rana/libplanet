using System;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using Libplanet.Action;
using Libplanet.Action.Loader;
using Libplanet.Action.State;
using Libplanet.Action.Tests.Common;
using Libplanet.Blockchain;
using Libplanet.Blockchain.Policies;
using Libplanet.Crypto;
using Libplanet.Mocks;
using Libplanet.Store;
using Libplanet.Store.Trie;
using Libplanet.Types.Assets;
using Libplanet.Types.Blocks;
using Libplanet.Types.Consensus;
using Libplanet.Types.Tx;
using Xunit;
using Xunit.Abstractions;

namespace Libplanet.Tests.Action
{
    public abstract class WorldTest
    {
        protected readonly PrivateKey[] _keys;

        /// <summary>
        /// A list of <see cref="Address"/>es used for testing derived from <c>_keys</c>.
        /// </summary>
        protected readonly Address[] _addr;

        /// <summary>
        /// A list of <see cref="Currency"><c>Currencies</c></see> used for testing:
        /// <list type="bullet">
        ///     <item><description>
        ///         <c>_currencies[0]</c>: Legacy C0L with <c>_addresses[0]</c> as its minter.
        ///     </description></item>
        ///     <item><description>
        ///         <c>_currencies[1]</c>: Legacy C1L with <c>_addresses[0]</c> and
        ///         <c>_addresses[1]</c> as its minters.
        ///     </description></item>
        ///     <item><description>
        ///         <c>_currencies[2]</c>: Legacy C2L with no minters.
        ///     </description></item>
        ///     <item><description>
        ///         <c>_currencies[3]</c>: Uncapped C3U with <c>_addresses[0]</c> as its minter.
        ///     </description></item>
        ///     <item><description>
        ///         <c>_currencies[4]</c>: Capped C4C with <c>_addresses[0]</c> as its minter.
        ///     </description></item>
        /// </list>
        /// Each <see cref="Currency"/> has zero decimal places.
        /// </summary>
        protected readonly Currency[] _currencies;

        /// <summary>
        /// An initial <see cref="IWorld"/> state set up for testing:
        /// <list type="bullet">
        ///     <item><description>
        ///         <c>_addresses[0]</c>: Has 5 C0L, 10 C1L, 5 C3U.
        ///     </description></item>
        ///     <item><description>
        ///         <c>_addresses[1]</c>: Has 15 C1L, 20 C2L.
        ///     </description></item>
        ///     <item><description>
        ///         Validators: Each address is a validator of power 1.
        ///     </description></item>
        /// </list>
        /// </summary>
        protected readonly IWorld _initWorld;
        protected readonly IActionContext _initContext;

        protected WorldTest(ITestOutputHelper output)
        {
            _keys = new[]
            {
                new PrivateKey(),
                new PrivateKey(),
                new PrivateKey(),
                new PrivateKey(),
            };

            _addr = _keys.Select(key => key.Address).ToArray();

            _currencies = new[]
            {
#pragma warning disable CS0618  // must test obsoleted Currency.Legacy() for backwards compatibility
                Currency.Legacy("C0L", 0, _addr[0]),
                Currency.Legacy("C1L", 0, _addr.Take(2).ToImmutableHashSet()),
                Currency.Legacy("C2L", 0, null),
#pragma warning restore CS0618  // must test obsoleted Currency.Legacy() for backwards compatibility
                Currency.Uncapped("C3U", 0, minter: _addr[0]),
                Currency.Capped("C4C", 0, (100, 0), minter: _addr[0]),
            };

            // FIXME: Should be tested on both legacy and modern.
            _initWorld = new World(MockWorldState.CreateLegacy()
                .SetBalance(_addr[0], _currencies[0], 5)
                .SetBalance(_addr[0], _currencies[1], 10)
                .SetBalance(_addr[0], _currencies[3], 5)
                .SetBalance(_addr[1], _currencies[1], 15)
                .SetBalance(_addr[1], _currencies[2], 20)
                .SetValidatorSet(new ValidatorSet(_keys
                    .Select(key => new Validator(key.PublicKey, 1))
                    .ToList())));

            output.WriteLine("Fixtures  {0,-42}  C0L  C1L  C2L  C3U  State  Validators", "Address");
            int i = 0;
            foreach (Address a in _addr)
            {
                output.WriteLine(
                    "_addr[{0}]  {1}  {2,3}  {3,3}  {4,3}  {5,3}  {6}",
                    i++,
                    a,
                    _initWorld.GetBalance(a, _currencies[0]),
                    _initWorld.GetBalance(a, _currencies[1]),
                    _initWorld.GetBalance(a, _currencies[2]),
                    _initWorld.GetBalance(a, _currencies[3]),
                    _initWorld.GetValidatorSet());
            }

            _initContext = CreateContext(_initWorld, _addr[0]);
        }

        public abstract int ProtocolVersion { get; }

        public abstract IActionContext CreateContext(IWorld world, Address signer);

        [Fact]
        public void InitialSetup()
        {
            Assert.Equal(Value(2, 20), _initWorld.GetBalance(_addr[1], _currencies[2]));
            Assert.Equal(Value(2, 0), _initWorld.GetBalance(_addr[2], _currencies[2]));
            Assert.Equal(Value(0, 5), _initWorld.GetBalance(_addr[0], _currencies[0]));
            Assert.Equal(Value(1, 10), _initWorld.GetBalance(_addr[0], _currencies[1]));
            Assert.Equal(Zero(2), _initWorld.GetBalance(_addr[0], _currencies[2]));
        }

        [Fact]
        public virtual void FungibleAssets()
        {
            IWorld a = _initWorld.TransferAsset(
                _initContext, _addr[1], _addr[2], Value(2, 5));
            Assert.Equal(Value(2, 15), a.GetBalance(_addr[1], _currencies[2]));
            Assert.Equal(Value(2, 5), a.GetBalance(_addr[2], _currencies[2]));
            Assert.Equal(Value(0, 5), a.GetBalance(_addr[0], _currencies[0]));
            Assert.Equal(Value(1, 10), a.GetBalance(_addr[0], _currencies[1]));
            Assert.Equal(Zero(2), a.GetBalance(_addr[0], _currencies[2]));
            Assert.Equal(Zero(0), a.GetBalance(_addr[1], _currencies[0]));
            Assert.Equal(Value(1, 15), a.GetBalance(_addr[1], _currencies[1]));
            Assert.Equal(Zero(0), a.GetBalance(_addr[2], _currencies[0]));
            Assert.Equal(Zero(1), a.GetBalance(_addr[2], _currencies[1]));

            var trieDiff = a.GetAccount(ReservedAddresses.LegacyAccount).Trie
                .Diff(_initContext.PreviousState.GetAccount(ReservedAddresses.LegacyAccount).Trie)
                .ToList();
            Assert.Single(trieDiff.Where(elem =>
                elem.Path.Equals(KeyConverters.ToFungibleAssetKey(_addr[1], _currencies[2]))));
            Assert.Single(trieDiff.Where(elem =>
                elem.Path.Equals(KeyConverters.ToFungibleAssetKey(_addr[2], _currencies[2]))));
            var accountDiff = AccountDiff.Create(
                _initContext.PreviousState.GetAccount(ReservedAddresses.LegacyAccount).Trie,
                a.GetAccount(ReservedAddresses.LegacyAccount).Trie);
            Assert.Empty(accountDiff.StateDiffs);
        }

        [Fact]
        public virtual void TransferAsset()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _initWorld.TransferAsset(_initContext, _addr[0], _addr[1], Zero(0))
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _initWorld.TransferAsset(_initContext, _addr[0], _addr[1], Value(0, -1))
            );
            Assert.Throws<InsufficientBalanceException>(() =>
                _initWorld.TransferAsset(_initContext, _addr[0], _addr[1], Value(0, 6))
            );

            IWorld a = _initWorld.TransferAsset(
                _initContext,
                _addr[0],
                _addr[1],
                Value(0, 6),
                allowNegativeBalance: true
            );
            Assert.Equal(Value(0, -1), a.GetBalance(_addr[0], _currencies[0]));
            Assert.Equal(Value(0, 6), a.GetBalance(_addr[1], _currencies[0]));
        }

        [Fact]
        public virtual BlockChain TransferAssetInBlock()
        {
            var store = new MemoryStore();
            var stateStore = new TrieStateStore(new MemoryKeyValueStore());
            var privateKey = new PrivateKey();
            BlockChain chain = TestUtils.MakeBlockChain(
                new NullBlockPolicy(),
                store,
                stateStore,
                new SingleActionLoader(typeof(DumbAction)),
                protocolVersion: ProtocolVersion,
                privateKey: privateKey
            );

            DumbAction action = DumbAction.Create((_addr[0], "a"), (_addr[1], _addr[0], 5));
            Transaction tx = Transaction.Create(
                0,
                _keys[0],
                chain.Genesis.Hash,
                new[] { action }.ToPlainValues()
            );
            var preEvalBlock = TestUtils.ProposeNext(
                chain.Tip,
                new[] { tx },
                miner: privateKey.PublicKey,
                protocolVersion: ProtocolVersion);
            var stateRootHash = chain.DetermineBlockStateRootHash(preEvalBlock, out _);
            var hash = preEvalBlock.Header.DeriveBlockHash(stateRootHash, null);
            Block block = ProtocolVersion < 2
                ? new Block(preEvalBlock, (stateRootHash, null, hash))
                : chain.EvaluateAndSign(preEvalBlock, privateKey);
            chain.Append(
                block,
                TestUtils.CreateBlockCommit(block)
            );
            Assert.Equal(
                DumbAction.DumbCurrency * 5,
                chain
                    .GetWorldState()
                    .GetBalance(_addr[0], DumbAction.DumbCurrency)
            );
            Assert.Equal(
                DumbAction.DumbCurrency * -5,
                chain
                    .GetWorldState()
                    .GetBalance(_addr[1], DumbAction.DumbCurrency)
            );

            return chain;
        }

        [Fact]
        public void MintAsset()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _initWorld.MintAsset(_initContext, _addr[0], Zero(0))
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _initWorld.MintAsset(_initContext, _addr[0], Value(0, -1))
            );

            IWorld delta0 = _initWorld;
            IActionContext context0 = _initContext;
            // currencies[0] (C0L) allows only _addr[0] to mint
            delta0 = delta0.MintAsset(context0, _addr[0], Value(0, 10));
            Assert.Equal(Value(0, 15), delta0.GetBalance(_addr[0], _currencies[0]));

            // currencies[1] (C1L) allows _addr[0] & _addr[1] to mint
            delta0 = delta0.MintAsset(context0, _addr[1], Value(1, 10));
            Assert.Equal(Value(1, 25), delta0.GetBalance(_addr[1], _currencies[1]));

            // currencies[2] (C2L) allows everyone to mint
            delta0 = delta0.MintAsset(context0, _addr[2], Value(2, 10));
            Assert.Equal(Value(2, 10), delta0.GetBalance(_addr[2], _currencies[2]));

            IWorld delta1 = _initWorld;
            IActionContext context1 = CreateContext(delta1, _addr[1]);
            // currencies[0] (C0L) disallows _addr[1] to mint
            Assert.Throws<CurrencyPermissionException>(() =>
                delta1.MintAsset(context1, _addr[1], Value(0, 10)));

            // currencies[1] (C1L) allows _addr[0] & _addr[1] to mint
            delta1 = delta1.MintAsset(context1, _addr[0], Value(1, 20));
            Assert.Equal(Value(1, 30), delta1.GetBalance(_addr[0], _currencies[1]));

            // currencies[2] (C2L) allows everyone to mint
            delta1 = delta1.MintAsset(context1, _addr[2], Value(2, 10));
            Assert.Equal(Value(2, 10), delta1.GetBalance(_addr[2], _currencies[2]));

            // currencies[4] (C4C) has a cap of 100
            Assert.Throws<SupplyOverflowException>(
                () => _initWorld.MintAsset(_initContext, _addr[0], Value(4, 200)));
        }

        [Fact]
        public virtual void BurnAsset()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _initWorld.BurnAsset(_initContext, _addr[0], Zero(0))
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _initWorld.BurnAsset(_initContext, _addr[0], Value(0, -1))
            );
            Assert.Throws<InsufficientBalanceException>(() =>
                _initWorld.BurnAsset(_initContext, _addr[0], Value(0, 6))
            );

            IWorld delta0 = _initWorld;
            IActionContext context0 = _initContext;
            // currencies[0] (C0L) allows only _addr[0] to burn
            delta0 = delta0.BurnAsset(context0, _addr[0], Value(0, 4));
            Assert.Equal(Value(0, 1), delta0.GetBalance(_addr[0], _currencies[0]));

            // currencies[1] (C1L) allows _addr[0] & _addr[1] to burn
            delta0 = delta0.BurnAsset(context0, _addr[1], Value(1, 10));
            Assert.Equal(Value(1, 5), delta0.GetBalance(_addr[1], _currencies[1]));

            // currencies[2] (C2L) allows everyone to burn
            delta0 = delta0.BurnAsset(context0, _addr[1], Value(2, 10));
            Assert.Equal(Value(2, 10), delta0.GetBalance(_addr[1], _currencies[2]));

            IWorld delta1 = _initWorld;
            IActionContext context1 = CreateContext(delta1, _addr[1]);
            // currencies[0] (C0L) disallows _addr[1] to burn
            Assert.Throws<CurrencyPermissionException>(() =>
                delta1.BurnAsset(context1, _addr[0], Value(0, 5)));

            // currencies[1] (C1L) allows _addr[0] & _addr[1] to burn
            delta1 = delta1.BurnAsset(context1, _addr[1], Value(1, 10));
            Assert.Equal(Value(1, 5), delta1.GetBalance(_addr[1], _currencies[1]));

            // currencies[2] (C2L) allows everyone to burn
            delta1 = delta1.BurnAsset(context1, _addr[1], Value(2, 10));
            Assert.Equal(Value(2, 10), delta1.GetBalance(_addr[1], _currencies[2]));
        }

        [Fact]
        public virtual void SetValidator()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                _initWorld.SetValidator(new Validator(new PrivateKey().PublicKey, -1))
            );

            var initCount = _keys.Length;
            var key3 = new PrivateKey().PublicKey;
            var key4 = new PrivateKey().PublicKey;

            IWorld delta = _initWorld;
            // delta already has 3 validators
            Assert.Equal(initCount, delta.GetValidatorSet().TotalCount);

            // nothing happens trying to delete non existing validator
            delta = delta.SetValidator(new Validator(key3, 0));
            Assert.Equal(initCount, delta.GetValidatorSet().TotalCount);

            // add key 3 to the validator set
            delta = delta.SetValidator(new Validator(key3, 1));
            Assert.Equal(initCount + 1, delta.GetValidatorSet().TotalCount);
            Assert.True(delta.GetValidatorSet().Contains(new Validator(key3, 1)));
            Assert.False(delta.GetValidatorSet().Contains(new Validator(key4, 1)));

            // add key 4 to the validator set
            delta = delta.SetValidator(new Validator(key4, 1));
            Assert.Equal(initCount + 2, delta.GetValidatorSet().TotalCount);
            Assert.True(delta.GetValidatorSet().Contains(new Validator(key3, 1)));
            Assert.True(delta.GetValidatorSet().Contains(new Validator(key4, 1)));

            // remove key 3 from the validator set
            delta = delta.SetValidator(new Validator(key3, 0));
            Assert.Equal(initCount + 1, delta.GetValidatorSet().TotalCount);
            Assert.False(delta.GetValidatorSet().Contains(new Validator(key3, 1)));
            Assert.True(delta.GetValidatorSet().Contains(new Validator(key4, 1)));

            // re-add key 3 to the validator set
            delta = delta.SetValidator(new Validator(key3, 1));
            Assert.Equal(initCount + 2, delta.GetValidatorSet().TotalCount);
            Assert.True(delta.GetValidatorSet().Contains(new Validator(key3, 1)));
            Assert.True(delta.GetValidatorSet().Contains(new Validator(key4, 1)));

            // remove all keys from the validator set
            delta = _keys.Aggregate(
                delta,
                (current, key) => current.SetValidator(new Validator(key.PublicKey, 0)));
            delta = delta.SetValidator(new Validator(key3, 0));
            delta = delta.SetValidator(new Validator(key4, 0));
            Assert.Equal(0, delta.GetValidatorSet().TotalCount);
        }

        protected FungibleAssetValue Value(int currencyIndex, BigInteger quantity) =>
            new FungibleAssetValue(_currencies[currencyIndex], quantity, 0);

        protected FungibleAssetValue Zero(int currencyIndex) => Value(currencyIndex, 0);
    }
}