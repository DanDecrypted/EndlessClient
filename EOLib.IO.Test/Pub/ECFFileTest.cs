﻿using EOLib.IO.Pub;
using EOLib.IO.Services;
using EOLib.IO.Services.Serializers;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace EOLib.IO.Test.Pub
{
    [TestFixture, ExcludeFromCodeCoverage]
    public class ECFFileTest
    {
        [Test]
        public void HasCorrectFileType()
        {
            Assert.That(new ECFFile().FileType, Is.EqualTo("ECF"));
        }

        [Test]
        public void SerializeToByteArray_ReturnsExpectedBytes()
        {
            var expectedBytes = MakeECFFile(55565554,
                new ECFRecord().WithID(1).WithName("TestFixture"),
                new ECFRecord().WithID(2).WithName("Test2"),
                new ECFRecord().WithID(3).WithName("Test3"),
                new ECFRecord().WithID(4).WithName("Test4"),
                new ECFRecord().WithID(5).WithName("Test5"),
                new ECFRecord().WithID(6).WithName("Test6"),
                new ECFRecord().WithID(7).WithName("Test7"),
                new ECFRecord().WithID(8).WithName("Test8"),
                new ECFRecord().WithID(9).WithName("eof"));

            var serializer = CreateFileSerializer();
            var file = serializer.DeserializeFromByteArray(expectedBytes, () => new ECFFile());

            var actualBytes = serializer.SerializeToByteArray(file, rewriteChecksum: false);

            CollectionAssert.AreEqual(expectedBytes, actualBytes);
        }

        [Test]
        public void DeserializeFromByteArray_HasExpectedIDAndNames()
        {
            var records = new[]
            {
                new ECFRecord().WithID(1).WithName("TestFixture"),
                new ECFRecord().WithID(2).WithName("Test2"),
                new ECFRecord().WithID(3).WithName("Test3"),
                new ECFRecord().WithID(4).WithName("Test4"),
                new ECFRecord().WithID(5).WithName("Test5"),
                new ECFRecord().WithID(6).WithName("Test6"),
                new ECFRecord().WithID(7).WithName("Test7"),
                new ECFRecord().WithID(8).WithName("Test8"),
                new ECFRecord().WithID(9).WithName("eof")
            };
            var bytes = MakeECFFile(55565554, records);

            var serializer = CreateFileSerializer();
            var file = serializer.DeserializeFromByteArray(bytes, () => new ECFFile());

            CollectionAssert.AreEqual(records.Select(x => new { x.ID, x.Name }).ToList(),
                                      file.Select(x => new { x.ID, x.Name }).ToList());
        }

        private byte[] MakeECFFile(int checksum, params IPubRecord[] records)
        {
            var numberEncoderService = new NumberEncoderService();

            var bytes = new List<byte>();
            bytes.AddRange(Encoding.ASCII.GetBytes("ECF"));
            bytes.AddRange(numberEncoderService.EncodeNumber(checksum, 4));
            bytes.AddRange(numberEncoderService.EncodeNumber(records.Length, 2));
            bytes.Add(numberEncoderService.EncodeNumber(1, 1)[0]);

            var recordSerializer = new PubRecordSerializer(numberEncoderService);
            foreach (var record in records)
                bytes.AddRange(recordSerializer.SerializeToByteArray(record));

            return bytes.ToArray();
        }

        private static IPubFileSerializer CreateFileSerializer()
        {
            return new PubFileSerializer(new NumberEncoderService(), new PubRecordSerializer(new NumberEncoderService()));
        }
    }
}
