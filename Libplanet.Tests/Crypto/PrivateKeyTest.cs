using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Libplanet.Common;
using Libplanet.Crypto;
using Xunit;
using static Libplanet.Tests.TestUtils;

namespace Libplanet.Tests.Crypto
{
    public class PrivateKeyTest
    {
        [Fact]
        public void FromString()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PrivateKey.FromString(string.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => PrivateKey.FromString("a"));
            Assert.Throws<ArgumentOutOfRangeException>(() => PrivateKey.FromString("870912"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PrivateKey.FromString(
                    "00000000000000000000000000000000000000000000000000000000870912"
                )
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PrivateKey.FromString(
                    "000000000000000000000000000000000000000000000000000000000000870912"
                )
            );
            Assert.Throws<FormatException>(() => PrivateKey.FromString("zz"));
            PrivateKey actual = PrivateKey.FromString(
                "e07107ca4b0d19147fa1152a0f2c7884705d59cbb6318e2f901bd28dd9ff78e3"
            );
            AssertBytesEqual(
                new byte[]
                {
                    0xe0, 0x71, 0x07, 0xca, 0x4b, 0x0d, 0x19, 0x14, 0x7f, 0xa1, 0x15,
                    0x2a, 0x0f, 0x2c, 0x78, 0x84, 0x70, 0x5d, 0x59, 0xcb, 0xb6, 0x31,
                    0x8e, 0x2f, 0x90, 0x1b, 0xd2, 0x8d, 0xd9, 0xff, 0x78, 0xe3,
                },
                actual.ToByteArray()
            );
        }

        [Fact]
        public void BytesTest()
        {
            var bs = new byte[]
            {
                0x98, 0x66, 0x98, 0x50, 0x72, 0x8c, 0x6c, 0x41, 0x0b, 0xf4,
                0x2c, 0x45, 0xfe, 0x7c, 0x49, 0x23, 0x2d, 0x14, 0xcf, 0xb5,
                0x5b, 0x78, 0x4d, 0x81, 0x35, 0xae, 0x40, 0x4c, 0x7c, 0x24,
                0x3f, 0xc7,
            };
            var key = new PrivateKey(bs);
            Assert.Equal(bs, key.ToByteArray());
            key = new PrivateKey(bs.ToImmutableArray());
            Assert.Equal(bs, key.ByteArray);
        }

        [Fact]
        public void BytesSanityCheckTest()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new PrivateKey(new byte[] { 0x87, 0x09, 0x12 })
             );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PrivateKey(new byte[31]
                {
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0x87, 0x09, 0x12,
                })
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PrivateKey(new byte[33]
                {
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
                    0x87, 0x09, 0x12,
                })
            );

            var bs = new byte[20]
            {
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0,
            };
            Assert.Throws<ArgumentException>(() => new PrivateKey(bs));
            ImmutableArray<byte> ibs = bs.ToImmutableArray();
            Assert.Throws<ArgumentException>(() => new PrivateKey(ibs));
        }

        [Fact]
        public void PublicKeyTest()
        {
            byte[] keyBytes =
            {
                0x82, 0xfc, 0x99, 0x47, 0xe8, 0x78, 0xfc, 0x7e, 0xd0, 0x1c,
                0x6c, 0x31, 0x06, 0x88, 0x60, 0x3f, 0x0a, 0x41, 0xc8, 0xe8,
                0x70, 0x4e, 0x5b, 0x99, 0x0e, 0x83, 0x88, 0x34, 0x3b, 0x0f,
                0xd4, 0x65,
            };
            var expected = new byte[]
            {
                0x04, 0xc7, 0xc6, 0x74, 0xa2, 0x23, 0x66, 0x1f, 0xae, 0xfe,
                0xd8, 0x51, 0x5f, 0xeb, 0xac, 0xc4, 0x11, 0xc0, 0xf3, 0x56,
                0x9c, 0x10, 0xc6, 0x5e, 0xc8, 0x6c, 0xdc, 0xe4, 0xd8, 0x5c,
                0x7e, 0xa2, 0x6c, 0x61, 0x7f, 0x0c, 0xf1, 0x9c, 0xe0, 0xb1,
                0x06, 0x86, 0x50, 0x1e, 0x57, 0xaf, 0x1a, 0x70, 0x02, 0x28,
                0x2f, 0xef, 0xa5, 0x28, 0x45, 0xbe, 0x22, 0x67, 0xd1, 0xf4,
                0xd7, 0xaf, 0x32, 0x29, 0x74,
            };

            Assert.Equal(expected, new PrivateKey(keyBytes).PublicKey.Format(false));
            Assert.Equal(
                expected,
                new PrivateKey(keyBytes.ToImmutableArray()).PublicKey.Format(false)
            );
        }

        [Fact]
        public void AddressTest()
        {
            var privateKey = new PrivateKey(
                new byte[]
                {
                    0xbe, 0xe6, 0xf9, 0xcc, 0x62, 0x41, 0x27, 0x60, 0xb3, 0x69, 0x6e,
                    0x05, 0xf6, 0xfb, 0x4a, 0xbe, 0xb9, 0xe8, 0x3c, 0x4f, 0x94, 0x4f,
                    0x83, 0xfd, 0x62, 0x08, 0x1b, 0x74, 0x54, 0xcb, 0xc0, 0x38,
                }
            );
            var expected = new Address("f45A22dD63f6428e85eE0a6E13a763278f57626d");
            Assert.Equal(expected, privateKey.Address);
        }

        [Fact]
        public void SignTest()
        {
            var pk = new PrivateKey(
                new byte[]
                {
                    0x52, 0x09, 0x38, 0xfa, 0xe0, 0x79, 0x78, 0x95, 0x61, 0x26,
                    0x8c, 0x29, 0x33, 0xf6, 0x36, 0xd8, 0xb5, 0xa0, 0x01, 0x1e,
                    0xa0, 0x41, 0x12, 0xdb, 0xab, 0xab, 0xf2, 0x95, 0xe5, 0xdd,
                    0xef, 0x88,
                }
            );
            var pubKey = pk.PublicKey;
            var wrongPubKey = new PrivateKey().PublicKey;
            var payload = new byte[]
            {
                0x64, 0x37, 0x3a, 0x61, 0x63, 0x74, 0x69, 0x6f, 0x6e, 0x73,
                0x6c, 0x65, 0x31, 0x30, 0x3a, 0x70, 0x75, 0x62, 0x6c, 0x69,
                0x63, 0x5f, 0x6b, 0x65, 0x79, 0x36, 0x35, 0x3a, 0x04, 0xb5,
                0xa2, 0x4a, 0xa2, 0x11, 0x27, 0x20, 0x42, 0x3b, 0xad, 0x39,
                0xa0, 0x20, 0x51, 0x82, 0x37, 0x9d, 0x6f, 0x2b, 0x33, 0xe3,
                0x48, 0x7c, 0x9a, 0xb6, 0xcc, 0x8f, 0xc4, 0x96, 0xf8, 0xa5,
                0x48, 0x34, 0x40, 0xef, 0xbb, 0xef, 0x06, 0x57, 0xac, 0x2e,
                0xf6, 0xc6, 0xee, 0x05, 0xdb, 0x06, 0xa9, 0x45, 0x32, 0xfd,
                0xa7, 0xdd, 0xc4, 0x4a, 0x16, 0x95, 0xe5, 0xce, 0x1a, 0x3d,
                0x3c, 0x76, 0xdb, 0x39, 0x3a, 0x72, 0x65, 0x63, 0x69, 0x70,
                0x69, 0x65, 0x6e, 0x74, 0x32, 0x30, 0x3a, 0x8a, 0xe7, 0x2e,
                0xfa, 0xb0, 0x95, 0x94, 0x66, 0x51, 0x12, 0xe6, 0xd4, 0x9d,
                0xfd, 0x19, 0x41, 0x53, 0x8c, 0xf3, 0x74, 0x36, 0x3a, 0x73,
                0x65, 0x6e, 0x64, 0x65, 0x72, 0x32, 0x30, 0x3a, 0xb6, 0xc0,
                0x3d, 0xe5, 0x7d, 0xdf, 0x03, 0x69, 0xc7, 0x20, 0x7d, 0x2d,
                0x11, 0x3a, 0xdf, 0xf8, 0x20, 0x51, 0x99, 0xcf, 0x39, 0x3a,
                0x74, 0x69, 0x6d, 0x65, 0x73, 0x74, 0x61, 0x6d, 0x70, 0x32,
                0x37, 0x3a, 0x32, 0x30, 0x31, 0x38, 0x2d, 0x30, 0x31, 0x2d,
                0x30, 0x32, 0x54, 0x30, 0x33, 0x3a, 0x30, 0x34, 0x3a, 0x30,
                0x35, 0x2e, 0x30, 0x30, 0x36, 0x30, 0x30, 0x30, 0x5a, 0x65,
            };

            // byte[] API:
            Assert.True(pubKey.Verify(payload, pk.Sign(payload)));
            Assert.False(pubKey.Verify(payload.Skip(1).ToArray(), pk.Sign(payload)));
            Assert.False(pubKey.Verify(payload, pk.Sign(payload).Skip(1).ToArray()));
            Assert.False(wrongPubKey.Verify(payload, pk.Sign(payload)));
            Assert.True(pubKey.Verify(payload, pk.Sign(payload)));

            // ImmutableArray<byte> API:
            var imPayload = payload.ToImmutableArray();
            Assert.False(pubKey.Verify(payload.Skip(1).ToArray(), pk.Sign(imPayload).ToArray()));
            Assert.False(pubKey.Verify(payload, pk.Sign(imPayload).Skip(1).ToArray()));
            Assert.False(wrongPubKey.Verify(payload, pk.Sign(imPayload).ToArray()));
        }

        [Fact]
        public void ExchangeTest()
        {
            PrivateKey prvKey = PrivateKey.FromString(
                "82fc9947e878fc7ed01c6c310688603f0a41c8e8704e5b990e8388343b0fd465"
            );
            byte[] pubkeyBytes = ByteUtil.ParseHex(
                "5f706787ac72c1080275c1f398640fb07e9da0b124ae9734b28b8d0f01eda586"
            );
            var pubKey = new PrivateKey(pubkeyBytes).PublicKey;

            var expected = new SymmetricKey(
                new byte[]
                {
                    0x59, 0x35, 0xd0, 0x47, 0x6a, 0xf9, 0xdf, 0x29, 0x98, 0xef,
                    0xb6, 0x03, 0x83, 0xad, 0xf2, 0xff, 0x23, 0xbc, 0x92, 0x83,
                    0x22, 0xcf, 0xbb, 0x73, 0x8f, 0xca, 0x88, 0xe4, 0x9d, 0x55,
                    0x7d, 0x7e,
                }
            );
            SymmetricKey actual = prvKey.ExchangeKey(pubKey);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Decrypt()
        {
            var prvKey = new PrivateKey(
                new byte[]
                {
                    0xfb, 0xc2, 0x00, 0x42, 0xb3, 0xa7, 0x07, 0xa7, 0xd5, 0xa1,
                    0xfa, 0x57, 0x71, 0x71, 0xf4, 0x9c, 0xd3, 0xa9, 0xe6, 0x7a,
                    0xb9, 0x29, 0x57, 0x57, 0xc7, 0x14, 0xe3, 0xf2, 0xf8, 0xc2,
                    0xd5, 0x73,
                }
            );
            var cipherText = new byte[]
            {
                0x03, 0xe3, 0x1a, 0x0d, 0xea, 0x31, 0xe2, 0xb1, 0x32, 0x7b,
                0xd8, 0x70, 0x0a, 0xd3, 0x57, 0xcc, 0x69, 0x31, 0x4e, 0xca,
                0xd7, 0x0a, 0xe2, 0xe4, 0xfa, 0x55, 0x17, 0xa3, 0x3b, 0x67,
                0xcf, 0xb1, 0xc4, 0xfa, 0xa1, 0x10, 0xd4, 0xd2, 0x73, 0x11,
                0xef, 0xf1, 0x47, 0x99, 0xd7, 0x3d, 0x3c, 0xaa, 0xa2, 0x0e,
                0x35, 0x7c, 0x41, 0xc8, 0x8e, 0x14, 0x22, 0xc7, 0x64, 0xed,
                0xcc, 0xe0, 0x6c, 0x06, 0xb5, 0x86, 0x44, 0xc1, 0x68, 0xa5,
                0xab, 0xf3, 0x9d, 0xcb, 0x46, 0xb6, 0xe2,
            };
            var expected = Encoding.ASCII.GetBytes("test message");

            Assert.Equal(expected, prvKey.Decrypt(cipherText));
            Assert.Equal(expected, prvKey.Decrypt(cipherText.ToImmutableArray()));
        }

        [Fact]
        public void DecryptDetectInvalidCipherText()
        {
            var key1 = new PrivateKey(
                new byte[]
                {
                    0xfb, 0xc2, 0x00, 0x42, 0xb3, 0xa7, 0x07, 0xa7, 0xd5, 0xa1,
                    0xfa, 0x57, 0x71, 0x71, 0xf4, 0x9c, 0xd3, 0xa9, 0xe6, 0x7a,
                    0xb9, 0x29, 0x57, 0x57, 0xc7, 0x14, 0xe3, 0xf2, 0xf8, 0xc2,
                    0xd5, 0x73,
                }
            );
            var key2 = new PrivateKey(
                new byte[]
                {
                    0xfb, 0xc2, 0x00, 0x42, 0xb3, 0xa7, 0x07, 0xa7, 0xd5, 0xa1,
                    0xfa, 0x57, 0x71, 0x71, 0xf4, 0x9c, 0xd3, 0xa9, 0xe6, 0x7a,
                    0xb9, 0x29, 0x57, 0x57, 0xc7, 0x14, 0xe3, 0xf2, 0xf8, 0xc2,
                    0xd5, 0x37,
                }
            );
            var message = Encoding.ASCII.GetBytes("test message");
            var cipherText = key1.PublicKey.Encrypt(message);

            Assert.Throws<InvalidCiphertextException>(() => key2.Decrypt(cipherText));
            Assert.Throws<InvalidCiphertextException>(
                () => key2.Decrypt(cipherText.ToImmutableArray())
            );
        }

        [Fact]
        public void EqualsTest()
        {
            var key1 = new PrivateKey(
                new byte[]
                {
                    0xfb, 0xc2, 0x00, 0x42, 0xb3, 0xa7, 0x07, 0xa7, 0xd5, 0xa1,
                    0xfa, 0x57, 0x71, 0x71, 0xf4, 0x9c, 0xd3, 0xa9, 0xe6, 0x7a,
                    0xb9, 0x29, 0x57, 0x57, 0xc7, 0x14, 0xe3, 0xf2, 0xf8, 0xc2,
                    0xd5, 0x73,
                }
            );
            var key2 = new PrivateKey(
                new byte[]
                {
                    0xfb, 0xc2, 0x00, 0x42, 0xb3, 0xa7, 0x07, 0xa7, 0xd5, 0xa1,
                    0xfa, 0x57, 0x71, 0x71, 0xf4, 0x9c, 0xd3, 0xa9, 0xe6, 0x7a,
                    0xb9, 0x29, 0x57, 0x57, 0xc7, 0x14, 0xe3, 0xf2, 0xf8, 0xc2,
                    0xd5, 0x73,
                }
            );
            var key3 = new PrivateKey(
                new byte[]
                {
                    0xfb, 0xc2, 0x00, 0x42, 0xb3, 0xa7, 0x07, 0xa7, 0xd5, 0xa1,
                    0xfa, 0x57, 0x71, 0x71, 0xf4, 0x9c, 0xd3, 0xa9, 0xe6, 0x7a,
                    0xb9, 0x29, 0x57, 0x57, 0xc7, 0x14, 0xe3, 0xf2, 0xf8, 0xc2,
                    0xd5, 0x37,
                }
            );

            Assert.Equal(key1, key2);
            Assert.NotEqual(key2, key3);

            Assert.True(key1 == key2);
            Assert.False(key2 == key3);

            Assert.False(key1 != key2);
            Assert.True(key2 != key3);
        }

        [Fact]
        public void HexStringConstructor()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrivateKey(string.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrivateKey("a"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PrivateKey("870912"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PrivateKey(
                    "00000000000000000000000000000000000000000000000000000000870912"
                )
            );
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PrivateKey(
                    "000000000000000000000000000000000000000000000000000000000000870912"
                )
            );
            Assert.Throws<FormatException>(() => new PrivateKey("zz"));
            PrivateKey actual = new PrivateKey(
                "e07107ca4b0d19147fa1152a0f2c7884705d59cbb6318e2f901bd28dd9ff78e3"
            );
            AssertBytesEqual(
                new byte[]
                {
                    0xe0, 0x71, 0x07, 0xca, 0x4b, 0x0d, 0x19, 0x14, 0x7f, 0xa1, 0x15,
                    0x2a, 0x0f, 0x2c, 0x78, 0x84, 0x70, 0x5d, 0x59, 0xcb, 0xb6, 0x31,
                    0x8e, 0x2f, 0x90, 0x1b, 0xd2, 0x8d, 0xd9, 0xff, 0x78, 0xe3,
                },
                actual.ToByteArray()
            );
        }

        [Fact]

        public void PrivateKeyGenerateLongerThan31Bytes()
        {
            var faults = new List<int>();
            for (int i = 0; i < 3000; i++)
            {
                var pk = new PrivateKey();

                if (pk.ByteArray.Length < 32)
                {
                    faults.Add(i);
                }
            }

            Assert.Empty(faults);
        }
    }
}
