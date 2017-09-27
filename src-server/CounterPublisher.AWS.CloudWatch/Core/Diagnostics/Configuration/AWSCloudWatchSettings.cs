// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AWSCloudWatchSettings.cs" company="Exit Games GmbH">
//   Copyright (c) Exit Games GmbH.  All rights reserved.
// </copyright>
// <summary>
//   Amazon WebServices CloudWatch settings.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ExitGames.Diagnostics.Configuration
{
    #region using directives

    using System;
    using System.Configuration;

    #endregion

    // ReSharper disable once InconsistentNaming
    public class AWSCloudWatchSettings : CounterSampleSenderSettings
    {
        #region Properties

        /// <summary>
        ///     The AWS Access Key associated with the account
        /// </summary>
        [ConfigurationProperty("awsCloudWatchAccessKey", IsRequired = true)]
        public string AWSCloudWatchAccessKey
        {
            get { return (string)this["awsCloudWatchAccessKey"]; }
            set { this["awsCloudWatchAccessKey"] = value; }
        }

        /// <summary>
        ///     The AWS Secret Access Key associated with the account
        /// </summary>
        [ConfigurationProperty("awsCloudWatchSecretKey", IsRequired = true)]
        public string AWSCloudWatchSecretKey
        {
            get { return (string)this["awsCloudWatchSecretKey"]; }
            set { this["awsCloudWatchSecretKey"] = value; }
        }

        /// <summary>
        ///     Regions and Endpoints: http://docs.aws.amazon.com/general/latest/gr/rande.html#cw_region
        /// 
        ///     Region Name	            Region	    Endpoint	                            Protocol
        ///     US East (N. Virginia)	us-east-1	monitoring.us-east-1.amazonaws.com      HTTP and HTTPS
        ///                                         logs.us-east-1.amazonaws.com            HTTPS
        /// 
        ///     US West (Oregon)	    us-west-2	monitoring.us-west-2.amazonaws.com      HTTP and HTTPS
        ///                                         logs.us-west-2.amazonaws.com            HTTPS
        /// 
        ///     US West (N. California)	us-west-1	monitoring.us-west-1.amazonaws.com      HTTP and HTTPS
        ///                                         logs.us-west-1.amazonaws.com            HTTPS
        /// 
        ///     EU (Ireland)	        eu-west-1	monitoring.eu-west-1.amazonaws.com      HTTP and HTTPS
        ///                                         logs.eu-west-1.amazonaws.com            HTTPS
        /// 
        ///     EU (Frankfurt)	        eu-central-1    
        ///                                         monitoring.eu-central-1.amazonaws.com   HTTP and HTTPS
        ///                                         logs.eu-central-1.amazonaws.com         HTTPS
        /// 
        ///     Asia Pacific (Singapore)
        ///                         	ap-southeast-1	
        ///                                         monitoring.ap-southeast-1.amazonaws.com HTTP and HTTPS
        ///                                         logs.ap-southeast-1.amazonaws.com       HTTPS
        /// 
        ///     Asia Pacific (Sydney)	ap-southeast-2	
        ///                                         monitoring.ap-southeast-2.amazonaws.com HTTP and HTTPS
        ///                                         logs.ap-southeast-2.amazonaws.com       HTTPS
        /// 
        ///     Asia Pacific (Tokyo)	ap-northeast-1	
        ///                                         monitoring.ap-northeast-1.amazonaws.com HTTP and HTTPS
        ///                                         logs.ap-northeast-1.amazonaws.com       HTTPS
        ///     South America (Sao Paulo)	
        ///                             sa-east-1	monitoring.sa-east-1.amazonaws.com	    HTTP and HTTPS
        /// </summary>
        [ConfigurationProperty("awsCloudWatchServiceUrl", IsRequired = true)]
        public Uri AWSCloudWatchServiceUrl
        {
            get
            {
                return (Uri)this["awsCloudWatchServiceUrl"];
            }
            set
            {
                this["awsCloudWatchServiceUrl"] = value;
                this["endpoint"] = value;
            }
        }

        [ConfigurationProperty("endpoint", IsRequired = false)]
        public new Uri Endpoint
        {
            get
            {
                return (Uri)this["endpoint"];
            }
            set
            {
                this["endpoint"] = value;
                this["awsCloudWatchServiceUrl"] = value;
            }
        }
        
        /// <summary>
        ///     Namespaces are containers for metrics. 
        ///     All AWS services that provide Amazon CloudWatch data use a namespace string, 
        ///     beginning with "AWS/". The following services push metric data points to CloudWatch.
        /// 
        ///     AWS Product	Namespace
        ///     Auto Scaling: AWS/AutoScaling, AWS Billing: AWS/Billing, Amazon CloudFront: AWS/CloudFront,
        ///     Amazon CloudSearch: AWS/CloudSearch, Amazon DynamoDB: AWS/DynamoDB, Amazon ElastiCache: AWS/ElastiCache,
        ///     Amazon Elastic Block Store: AWS/EBS, Amazon Elastic Compute Cloud: AWS/EC2,
        ///     Elastic Load Balancing: AWS/ELB, Amazon Elastic MapReduce: AWS/ElasticMapReduce,
        ///     Amazon Kinesis: AWS/Kinesis, AWS OpsWorks: AWS/OpsWorks, Amazon Redshift: AWS/Redshift,
        ///     Amazon Relational Database Service: AWS/RDS, Amazon Route 53: AWS/Route53,
        ///     Amazon Simple Notification Service: AWS/SNS, Amazon Simple Queue Service: AWS/SQS,
        ///     Amazon Simple Workflow Service: AWS/SWF, AWS Storage Gateway: AWS/StorageGateway
        /// </summary>
        [ConfigurationProperty("awsCloudWatchNamespace", IsRequired = true)]
        public string AWSCloudWatchNamespace
        {
            get { return (string)this["awsCloudWatchNamespace"]; }
            set { this["awsCloudWatchNamespace"] = value; }
        }

        /// <summary>
        ///     If you want Photon metrics with format: Namespace.Metric -> to be written to
        ///     Namespace = awsCloudWatchNamespace/Namespace, Name = Metric
        /// </summary>
        [ConfigurationProperty("autoNamespace", IsRequired = false, DefaultValue = false)]
        public bool AutoNamespace
        {
            get { return (bool)this["autoNamespace"]; }
            set { this["autoNamespace"] = value; }
        }

        /// <summary>
        ///     Dimension of InstanceId
        /// </summary>
        [ConfigurationProperty("awsInstanceIdLookupUrl", IsRequired = false)]
        public string AWSInstanceIdLookupUrl
        {
            get { return (string)this["awsInstanceIdLookupUrl"]; }
            set { this["awsInstanceIdLookupUrl"] = value; }
        }

        /// <summary>
        ///     Dimension of AutoScalingGroup
        /// </summary>
        [ConfigurationProperty("autoScalingConfigFilePath", IsRequired = false)]
        public string AutoScalingConfigFilePath
        {
            get { return (string)this["autoScalingConfigFilePath"]; }
            set { this["autoScalingConfigFilePath"] = value; }
        }

        #endregion
    }
}
