using System;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using Libplanet.Blocks;
using Libplanet.Crypto;
using Libplanet.Net.Messages;
using Libplanet.Net.Transports;
using Libplanet.Tests.Common.Action;
using NetMQ;
using Xunit;
using static Libplanet.Tests.TestUtils;

namespace Libplanet.Net.Tests.Messages
{
    public class MessageTest
    {
        [Fact]
        public void BlockHeaderMsg()
        {
            var privateKey = new PrivateKey();
            var peer = new BoundPeer(privateKey.PublicKey, new DnsEndPoint("0.0.0.0", 0));
            var apv = new AppProtocolVersion(
                1,
                new Bencodex.Types.Integer(0),
                ImmutableArray<byte>.Empty,
                default(Address));
            var dateTimeOffset = DateTimeOffset.UtcNow;
            Block<DumbAction> genesis = MineGenesisBlock<DumbAction>(GenesisMiner);
            var message = new BlockHeaderMsg(genesis.Hash, genesis.Header);
            var codec = new NetMQMessageCodec();
            NetMQMessage raw =
                codec.Encode(message, privateKey, apv, peer, dateTimeOffset);
            var parsed = codec.Decode(raw, true);
            Assert.Equal(peer, parsed.Remote);
        }

        [Fact]
        public void InvalidCredential()
        {
            var message = new PingMsg();
            var privateKey = new PrivateKey();
            var apv = new AppProtocolVersion(
                1,
                new Bencodex.Types.Integer(0),
                ImmutableArray<byte>.Empty,
                default(Address));
            var peer = new BoundPeer(privateKey.PublicKey, new DnsEndPoint("0.0.0.0", 0));
            var timestamp = DateTimeOffset.UtcNow;
            var badPrivateKey = new PrivateKey();
            var codec = new NetMQMessageCodec();
            Assert.Throws<InvalidCredentialException>(() =>
                codec.Encode(message, badPrivateKey, apv, peer, timestamp));
        }

        [Fact]
        public void UseInvalidSignature()
        {
            // Victim
            var privateKey = new PrivateKey();
            var peer = new BoundPeer(privateKey.PublicKey, new DnsEndPoint("0.0.0.0", 0));
            var timestamp = DateTimeOffset.UtcNow;
            var apv = new AppProtocolVersion(
                1,
                new Bencodex.Types.Integer(0),
                ImmutableArray<byte>.Empty,
                default(Address));
            var ping = new PingMsg();
            var codec = new NetMQMessageCodec();
            var netMqMessage = codec.Encode(ping, privateKey, apv, peer, timestamp).ToArray();

            // Attacker
            var fakePeer = new BoundPeer(privateKey.PublicKey, new DnsEndPoint("1.2.3.4", 0));
            var fakeMessage = codec.Encode(ping, privateKey, apv, fakePeer, timestamp).ToArray();

            var frames = new NetMQMessage();
            frames.Push(netMqMessage[4]);
            frames.Push(netMqMessage[3]);
            frames.Push(fakeMessage[2]);
            frames.Push(netMqMessage[1]);
            frames.Push(netMqMessage[0]);

            Assert.Throws<InvalidMessageSignatureException>(() =>
                codec.Decode(frames, true));
        }

        [Fact]
        public void InvalidArguments()
        {
            var codec = new NetMQMessageCodec();
            var message = new PingMsg();
            var privateKey = new PrivateKey();
            var apv = new AppProtocolVersion(
                1,
                new Bencodex.Types.Integer(0),
                ImmutableArray<byte>.Empty,
                default(Address));
            Assert.Throws<ArgumentException>(
                () => codec.Decode(new NetMQMessage(), true));
        }

        [Fact]
        public void GetId()
        {
            var privateKey = new PrivateKey();
            var peer = new BoundPeer(privateKey.PublicKey, new DnsEndPoint("1.2.3.4", 1234));
            var apv = new AppProtocolVersion(
                1,
                new Bencodex.Types.Integer(0),
                ImmutableArray<byte>.Empty,
                default(Address));
            var dateTimeOffset = DateTimeOffset.MinValue + TimeSpan.FromHours(6.1234);
            Block<DumbAction> genesis = MineGenesisBlock<DumbAction>(GenesisMiner);
            var message = new BlockHeaderMsg(genesis.Hash, genesis.Header)
            {
                Timestamp = dateTimeOffset,
                Remote = peer,
            };
            Assert.Equal(
                new MessageId(new byte[32]
                {
                    0xf2, 0xe6, 0x2d, 0xbd, 0x95, 0xd0, 0x0f, 0x5a,
                    0x54, 0xf2, 0xf2, 0xe3, 0x76, 0xc1, 0x93, 0xb3,
                    0x21, 0x05, 0x93, 0x75, 0x39, 0xe6, 0x84, 0xc5,
                    0x55, 0x0a, 0x23, 0x75, 0xbc, 0x38, 0xdc, 0x11,
                }),
                message.Id);
        }
    }
}
