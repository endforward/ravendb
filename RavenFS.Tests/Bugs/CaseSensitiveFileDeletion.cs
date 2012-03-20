﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace RavenFS.Tests.Bugs
{
    public class CaseSensitiveFileDeletion : WebApiTest
    {
        [Fact]
        public void FilesWithUpperCaseNamesAreDeletedProperly()
        {
            var client = NewClient();
            var ms = new MemoryStream();
            client.UploadAsync("Abc.txt", ms).Wait();

            client.DeleteAsync("Abc.txt").Wait();

            var result = client.GetFilesAsync("/").Result;

            Assert.Equal(0, result.FileCount);
        }
    }
}
