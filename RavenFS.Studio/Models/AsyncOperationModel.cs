﻿using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using RavenFS.Studio.Extensions;
using RavenFS.Studio.Infrastructure;

namespace RavenFS.Studio.Models
{
    public class AsyncOperationModel : Model
    {
        string description;
        double progress;
        string error;
        bool reportsProgress;
        Exception exception;
        AsyncOperationStatus status;

        public AsyncOperationModel()
        {
            Status = AsyncOperationStatus.Queued;
        }

        public string Description
        {
            get { return description; }
            set
            {
                description = value;
                OnPropertyChanged("Name");
            }
        }

        public double Progress
        {
            get { return progress; }
            private set
            {
                progress = value;
                OnPropertyChanged("Progress");
            }
        }

        public AsyncOperationStatus Status
        {
            get { return status; }
            private set
            {
                status = value;
                OnPropertyChanged("Status");
            }
        }

        public string Error
        {
            get { return error; }
            private set
            {
                error = value;
                OnPropertyChanged("Error");
            }
        }

        public Exception Exception
        {
            get { return exception; }
            set
            {
                exception = value;
                OnPropertyChanged("Exception");
            }
        }

        public void ProgressChanged(double amountCompleted, double amountToDo)
        {
            ProgressChanged((amountCompleted / amountToDo).Clamp(0, 1));
        }

        public void ProgressChanged(double progress)
        {
            if (progress < 0 || progress > 1)
            {
                throw new ArgumentOutOfRangeException("progress", "progress must be between 0 and 1");
            }

            if (Status == AsyncOperationStatus.Queued)
            {
                Started();
            }

            Progress = progress;
        }

        public void Started()
        {
            Status = AsyncOperationStatus.Processing;
        }

        public void Completed()
        {
            Status = AsyncOperationStatus.Completed;
            Progress = 0;
        }

        public void Faulted(Exception exception)
        {
            Status = AsyncOperationStatus.Error;
            if (exception != null)
            {
                Exception = exception;

                Error = exception is AggregateException
                            ? ((exception as AggregateException).ExtractSingleInnerException() ?? exception).Message
                            : exception.Message;
            }

            Progress = 0;
        }
    }
}
