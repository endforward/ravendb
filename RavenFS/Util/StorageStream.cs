﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using RavenFS.Storage;

namespace RavenFS.Util
{
    public class StorageStream : Stream
    {
        public TransactionalStorage TransactionalStorage { get; private set; }

        private FileHeader fileHeader;
        public string Name
        {
            get { return fileHeader.Name; }
        }
        public long Size { get { return fileHeader.TotalSize.Value; } }

        private const int MaxPageSize = 64 * 1024;
        private const int PagesBatchSize = 64;
        private FileAndPages fileAndPages;
        private long currentOffset;
        private long currentPageFrameSize { get { return fileAndPages.Pages.Sum(item => item.Size); } }
        private long currentPageFrameOffset;
        private bool EndOfPages { get { return fileAndPages.Pages.Count < PagesBatchSize; } }

        public StorageStream(TransactionalStorage transactionalStorage, string fileName)
        {
            TransactionalStorage = transactionalStorage;
            TransactionalStorage.Batch(accessor => fileHeader = accessor.ReadFile(fileName));
            if (fileHeader.TotalSize == null)
            {
                throw new FileNotFoundException("File is not uploaded yet");
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    break;
                case SeekOrigin.Current:
                    offset = currentOffset + offset;
                    break;
                case SeekOrigin.End:
                    offset = Size - offset - 1;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("origin");
            }
            MovePageFrame(offset);
            return currentOffset;
        }

        private void MovePageFrame(long offset)
        {
            offset = Math.Min(Size - 1, offset);
            if (offset < currentPageFrameOffset)
            {
                TransactionalStorage.Batch(accessor => fileAndPages = accessor.GetFile(Name, 0, PagesBatchSize));
                currentPageFrameOffset = 0;
            }
            while (currentPageFrameOffset + currentPageFrameSize - 1 < offset)
            {
                var nextPageIndex = fileAndPages.Start + fileAndPages.Pages.Count;
                TransactionalStorage.Batch(accessor => fileAndPages = accessor.GetFile(Name, nextPageIndex, PagesBatchSize));
                currentPageFrameOffset += currentPageFrameSize;
            }
            currentOffset = offset;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (currentOffset >= Size)
            {
                return 0;
            }
            var innerBuffer = new byte[MaxPageSize];
            var pageOffset = currentPageFrameOffset;
            var length = 0;
            foreach (var page in fileAndPages.Pages)
            {
                if (pageOffset <= currentOffset && currentOffset < pageOffset + page.Size)
                {
                    TransactionalStorage.Batch(accessor => length = accessor.ReadPage(page.Key, innerBuffer));
                    var sourceIndex = currentOffset - pageOffset;
                    length = Math.Min(length, Math.Min(buffer.Length - offset, count));
                    Array.Copy(innerBuffer, sourceIndex, buffer, offset, length);
                    break;
                }
                pageOffset += page.Size;
            }
            MovePageFrame(currentOffset + length);
            return length;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead
        {
            get { return fileHeader.TotalSize.HasValue; }
        }

        public override bool CanSeek
        {
            get { return fileHeader.TotalSize.HasValue; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return fileHeader.TotalSize ?? 0; }
        }

        public override long Position
        {
            get { return currentOffset; }
            set { Seek(value, SeekOrigin.Begin); }
        }
    }
}