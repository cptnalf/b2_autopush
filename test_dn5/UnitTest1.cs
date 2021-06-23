using NUnit.Framework;

namespace test_dn5
{
    using System.Security.Cryptography;

    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var key = RSA.Create();
            var chars = System.IO.File.ReadAllText("/data2/temp/new_rsa.pub");

            key.ImportFromPem(chars);

            Assert.That(key, Is.Not.Null);
        }

        [Test]
        public void RSAPkeyTest()
        {
            var key = RSA.Create();
            var chars = System.IO.File.ReadAllText("/data2/temp/new_rsa");

            key.ImportFromPem(chars);
            Assert.That(key, Is.Not.Null);
        }
    }
}