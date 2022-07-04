using System;
namespace Producer
{
    public class AWSConfig
    {
        public const string AWS = "AWS";
        public string Key { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Profile { get; set; } = string.Empty;
        public string QueueUrl { get; set; } = string.Empty;
    }
}
