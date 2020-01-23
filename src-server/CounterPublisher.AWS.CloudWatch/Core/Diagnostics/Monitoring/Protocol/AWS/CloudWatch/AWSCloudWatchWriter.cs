// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AWSCloudWatchWriter.cs" company="Exit Games GmbH">
//   Copyright (c) Exit Games GmbH.  All rights reserved.
// </copyright>
// <summary>
//   Amazon WebServices CloudWatch writer.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ExitGames.Diagnostics.Monitoring.Protocol.AWS.CloudWatch
{
    #region using directives

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Globalization;
    
    using Amazon.CloudWatch;
    using Amazon.CloudWatch.Model;
    
    using ExitGames.Logging;
    using ExitGames.Diagnostics.Configuration;
    
    #endregion

    /// <summary>
    /// Publishes metric data points to Amazon CloudWatch. 
    /// Amazon Cloudwatch associates the data points with the specified metric. 
    /// If the specified metric does not exist, Amazon CloudWatch creates the metric. 
    /// It can take up to fifteen minutes for a new metric to appear in calls to the list-metrics action.
    /// 
    /// The size of a put-metric-datarequest is limited to 8 KB for HTTP GET requests and 40 KB for HTTP POST requests.
    /// 
    /// Although you can publish data points with time stamps as granular as one-thousandth of a second, 
    /// CloudWatch aggregates the data to a minimum granularity of one minute. 
    /// CloudWatch records the average (sum of all items divided by number of items) of the values 
    /// received for every 1-minute period, as well as number of samples, maximum value, 
    /// and minimum value for the same time period.
    /// </summary>
    public class AWSCloudWatchWriter : ICounterSampleWriter
    {
        #region Constants and Fields

        private static readonly ILogger Log = LogManager.GetCurrentClassLogger();

        public const int MaxMetricNameLentgh = 255;

        public const int MinSendInterval = 60;

        public const int MaxPayloadSize = 40*1024;

        // cloud-watch only lets us send maximum of 20 pieces of metric data per request,
        // so split in to groups of 20
        public const int MetricsMaxBatchSize = 20;

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        private static readonly Regex SpecialCharPattern = new Regex("[^a-zA-Z0-9]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly StandardUnit[] DataMeasure =
        {
            StandardUnit.Bits, StandardUnit.Bytes, StandardUnit.Gigabits, StandardUnit.Gigabytes,
            StandardUnit.Kilobits, StandardUnit.Kilobytes, StandardUnit.Megabits, StandardUnit.Megabytes,
            StandardUnit.Terabits, StandardUnit.Terabytes
        };
        
        private static readonly StandardUnit[] OtherMeasure =
        {
            StandardUnit.Microseconds, StandardUnit.Milliseconds, StandardUnit.Seconds,
            StandardUnit.Count, StandardUnit.Percent
        };

        private readonly AWSCloudWatchSettings settings;
        
        private CounterSampleSenderBase sender;

        private bool _disposed;

        private string instanceId = Environment.MachineName;

        private string autoScalingGroupName = null;

        private IAmazonCloudWatch cloudWatchClient;
        
        #endregion

        #region Properties

        public bool Ready
        {
            get { return true; }
        }

        public string SenderId
        {
            get { return this.sender.SenderId; }
        }

        #endregion

        #region Constructors and Destructors

        public AWSCloudWatchWriter(AWSCloudWatchSettings s)
        {
            this.settings = s;
        }

        ~AWSCloudWatchWriter()
        {
            this.Dispose(false);
        }

        #endregion

        #region Methods
        
        public void Start(CounterSampleSenderBase sender)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Resource was disposed.");
            }

            if (this.sender != null)
            {
                throw new InvalidOperationException("Already started(), can't call for second time");
            }

            this.sender = sender;

            if (this.sender.SendInterval < MinSendInterval)
            {
                throw new ArgumentOutOfRangeException("sender", "sender.SendInterval is out of range. Min value is " + MinSendInterval);
            }

            // Dimension of InstanceId
            if (!String.IsNullOrEmpty(this.settings.AWSInstanceIdLookupUrl))
            {
                try
                {
                    this.instanceId = new WebClient().DownloadString(this.settings.AWSInstanceIdLookupUrl);
                }
                catch (Exception e)
                {
                    // This will fail if running machine is not in AWS EC2
                    Log.Error(e);
                    Log.WarnFormat(
                        "Failed to retrieve AWS instance id. Use hostname {0} instead. Lookup URL was: {1}",
                        this.instanceId,
                        this.settings.AWSInstanceIdLookupUrl);
                }
            }

            // Dimension of AutoScalingGroup
            if (!String.IsNullOrEmpty(this.settings.AutoScalingConfigFilePath))
            {
                try
                {
                    var autoScalingConfigFile = new FileInfo(this.settings.AutoScalingConfigFilePath);
                    if (!autoScalingConfigFile.Exists)
                    {
                        Log.WarnFormat("AutoScalingConfigFile not found: {0}",
                            this.settings.AutoScalingConfigFilePath);
                    }
                    else
                    {
                        using (var sr = new StreamReader(autoScalingConfigFile.FullName))
                        {
                            this.autoScalingGroupName = sr.ReadToEnd().Trim();
                        }
                        
                        Log.InfoFormat(
                            "AutoScalingGroupName {0} read from config file {1}",
                            this.autoScalingGroupName,
                            autoScalingConfigFile.FullName);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    Log.WarnFormat(
                        "Failed to read AutoScalingGroupName from config file {0}",
                        this.settings.AutoScalingConfigFilePath);
                }
            }

            // Amazon AWS CloudWatch Client init
            this.cloudWatchClient =
                new AmazonCloudWatchClient(
                    this.settings.AWSCloudWatchAccessKey,
                    this.settings.AWSCloudWatchSecretKey, new AmazonCloudWatchConfig
                    {
                        ServiceURL = this.settings.AWSCloudWatchServiceUrl.OriginalString
                    });
        }

        public void Publish(CounterSampleCollection[] packages)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("Resource was disposed.");
            }

            if (!this.Ready)
            {
                sender.RaiseOnDisconnetedEvent();
                return;
            }

            var data = new List<MetricDatum>(packages.Length);
            foreach (var package in packages)
            {
                var d = GetMetricFromPerformanceCounterReader(package);
                if (d.MetricName.Length > MaxMetricNameLentgh)
                {
                    Log.WarnFormat("Metric is too long (max 255): {0}, {1}", d.MetricName, GetMetricDatumToString(d));
                    continue;
                }
                data.Add(d);
                if (data.Count == MetricsMaxBatchSize)
                {
                    PublishMetricData(data);
                    data.Clear();
                }
            }
            // publish remaining (% MetricsMaxBatchSize) if any
            PublishMetricData(data);
        }

        private void PublishMetricData(List<MetricDatum> data)
        {
            if (data.Count <= 0) return;

            if (this.settings.AutoNamespace)
            {
                while (data.Count > 0)
                {
                    var first = data[data.Count - 1];
                    data.RemoveAt(data.Count - 1);

                    var nsi = first.MetricName.LastIndexOf('.');
                    if (nsi > 0)
                    {
                        var ns = first.MetricName.Substring(0, nsi).Replace('.', '/');
                        first.MetricName = first.MetricName.Substring(nsi + 1);

                        var nons = new PutMetricDataRequest()
                        {
                            MetricData = new List<MetricDatum>()
                            {
                                first
                            },
                            Namespace = this.settings.AWSCloudWatchNamespace + '/' + ns
                        };

                        for (var i = data.Count - 1; i >= 0; i--)
                        {
                            var d = data[i];
                            if (d.MetricName.StartsWith(ns))
                            {
                                d.MetricName = d.MetricName.Substring(nsi + 1);
                                nons.MetricData.Add(d);
                                data.RemoveAt(i);
                            }
                        }

                        if (Log.IsDebugEnabled)
                        {
                            foreach (var d in nons.MetricData)
                            {
                                Log.DebugFormat("Report metric at ns://{0}: {1}, {2}", nons.Namespace, d.MetricName, GetMetricDatumToString(d));
                            }
                        }

                        this.cloudWatchClient.PutMetricData(nons);
                    }
                    else
                    {
                        // Collect all without dots
                        var nons = new PutMetricDataRequest()
                        {
                            MetricData = new List<MetricDatum>()
                            {
                                first
                            },
                            Namespace = this.settings.AWSCloudWatchNamespace
                        };

                        for (var i = data.Count - 1; i >= 0; i --)
                        {
                            if (data[i].MetricName.IndexOf('.') < 0)
                            {
                                nons.MetricData.Add(data[i]);
                                data.RemoveAt(i);
                            }
                        }

                        if (Log.IsDebugEnabled)
                        {
                            foreach (var d in nons.MetricData)
                            {
                                Log.DebugFormat("Report metric at ns://{0}, {1}, {2}", nons.Namespace, d.MetricName, GetMetricDatumToString(d));
                            }
                        }

                        this.cloudWatchClient.PutMetricData(nons);
                    }
                }
            }
            else
            {
                if (Log.IsDebugEnabled)
                {
                    foreach (var d in data)
                    {
                        Log.DebugFormat("Report metric: {0}, {1}", d.MetricName, GetMetricDatumToString(d));
                    }
                }

                this.cloudWatchClient.PutMetricData(
                    new PutMetricDataRequest()
                    {
                        MetricData = data,
                        Namespace = this.settings.AWSCloudWatchNamespace
                    });
            }
        }

        private static string GetMetricDatumToString(MetricDatum d)
        {
            var s = new StringBuilder();

            s.Append("(Metric");

            if (d.MetricName != null)
            {
                s.AppendFormat(", name = {0}", d.MetricName);
            }

            if (d.StatisticValues != null)
            {
                s.AppendFormat(", stats = (min {0}, max {1}, count {2}, sum {3})", 
                    d.StatisticValues.Minimum,
                    d.StatisticValues.Maximum,
                    d.StatisticValues.SampleCount,
                    d.StatisticValues.Sum);
            }

            if (d.StatisticValues == null)
            {
                s.AppendFormat(", val = {0}", d.Value);
            }

            if (d.Unit != null)
            {
                s.AppendFormat(", unit = {0}", d.Unit.Value);
            }

            s.AppendFormat(", ts = {0}", d.Timestamp.ToString(CultureInfo.InvariantCulture));
            s.Append(')');

            return s.ToString();
        }

        private MetricDatum GetMetricFromPerformanceCounterReader(CounterSampleCollection package)
        {
            var data = new MetricDatum()
            {
                MetricName = package.CounterName,
                StatisticValues = new StatisticSet()
                {
                    Maximum = 0.0,
                    Minimum = 0.0,
                    SampleCount = 0.0,
                    Sum = 0.0
                },
                Unit = StandardUnit.Count,
                Timestamp = Epoch,
                Dimensions = new List<Dimension>(3)
            };

            // Find unit
            for (int i = 0; i < DataMeasure.Length; i++)
            {
                var u = DataMeasure[i];
                if (data.MetricName.Contains(u.Value))
                {
                    data.Unit = u;
                }
            }

            if (data.MetricName.IndexOf("PerSec", StringComparison.InvariantCultureIgnoreCase) > -1)
            {
                data.Unit = data.Unit.Value + "/Second";
            }
            else
            {
                for (int i = 0; i < OtherMeasure.Length; i++)
                {
                    var u = OtherMeasure[i];
                    if (data.MetricName.Contains(u.Value))
                    {
                        data.Unit = u;
                    }
                }
            }
            
            // Collect stats
            foreach (var sample in package)
            {
                data.StatisticValues.Maximum = Math.Max(data.StatisticValues.Maximum, sample.Value);
                data.StatisticValues.Minimum = Math.Min(data.StatisticValues.Minimum, sample.Value);
                data.StatisticValues.SampleCount += 1.0;
                data.StatisticValues.Sum += sample.Value;

                if (data.Timestamp < sample.Timestamp)
                {
                    data.Timestamp = sample.Timestamp;
                }
            }

            // Setup dimensions
            if (!String.IsNullOrEmpty(this.instanceId))
            {
                data.Dimensions.Add(new Dimension()
                {
                    Name = "InstanceId",
                    Value = this.instanceId
                });
            }

            if (!String.IsNullOrEmpty(this.autoScalingGroupName))
            {
                data.Dimensions.Add(new Dimension()
                {
                    Name = "AutoScalingGroupName",
                    Value = this.autoScalingGroupName
                });
            }

            if (!String.IsNullOrEmpty(this.SenderId))
            {
                data.Dimensions.Add(new Dimension()
                {
                    Name = "SenderId",
                    Value = this.SenderId
                });
            }

            return data;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (this.cloudWatchClient != null)
                    {
                        this.cloudWatchClient.Dispose();
                        this.cloudWatchClient = null;
                    }

                    this.sender = null;
                }

                _disposed = true;
            }
        }


        #endregion
    }
}
