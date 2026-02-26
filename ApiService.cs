using System.Collections.Generic;
using System.Threading.Tasks;

namespace autoquest
{
    public class ApiService
    {
        public ApiService(string url) { }
        public Task<bool> HealthCheckAsync() => Task.FromResult(true);
        public Task<ActivationResponse> ActivateAsync(string fingerprint) => Task.FromResult(new ActivationResponse());
        public Task<VerifyResponse> VerifyAsync(string token) => Task.FromResult(new VerifyResponse { valid = true });
        public Task<ExtractResponse> ExtractAsync(string token, List<string> images, List<string> variables) => Task.FromResult(new ExtractResponse());
    }

    public class ActivationResponse
    {
        public string token { get; set; } = "dummy";
        public string expires_at { get; set; } = System.DateTime.Now.AddDays(1).ToString();
        public int quota_remaining { get; set; } = 100;
    }

    public class VerifyResponse
    {
        public bool valid { get; set; }
        public int remaining { get; set; } = 100;
    }

    public class ExtractResponse
    {
        public int remaining { get; set; } = 99;
    }
}
