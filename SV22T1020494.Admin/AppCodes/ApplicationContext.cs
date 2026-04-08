using System;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace SV22T1020494.Admin
{
    /// <summary>
    /// Lớp cung cấp các tiện ích liên quan đến ngữ cảnh của ứng dụng web
    /// </summary>
    public static class ApplicationContext
    {
        private static IHttpContextAccessor? _httpContextAccessor;
        private static IWebHostEnvironment? _webHostEnvironment;
        private static IConfiguration? _configuration;

        /// <summary>
        /// Gọi hàm này trong Program
        /// </summary>
        /// <param name="httpContextAccessor">app.Services.GetRequiredService<IHttpContextAccessor>()</param>
        /// <param name="webHostEnvironment">app.Services.GetRequiredService<IWebHostEnvironment>()</param>
        /// <param name="configuration">app.Configuration</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void Configure(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// HttpContext
        /// </summary>
        public static HttpContext? HttpContext => _httpContextAccessor?.HttpContext;
        /// <summary>
        /// WebHostEnviroment
        /// </summary>
        public static IWebHostEnvironment? WebHostEnviroment => _webHostEnvironment;
        /// <summary>
        /// Configuration
        /// </summary>
        public static IConfiguration? Configuration => _configuration;

        /// <summary>
        /// URL của website, kết thúc bởi dấu / (ví dụ: https://mywebsite.com/)
        /// </summary>
        public static string BaseURL => $"{HttpContext?.Request.Scheme}://{HttpContext?.Request.Host}/";
        /// <summary>
        /// Đường dẫn vật lý đến thư mục wwwroot
        /// </summary>
        public static string WWWRootPath => _webHostEnvironment?.WebRootPath ?? string.Empty;
        /// <summary>
        /// Đường dẫn vật lý đến thư mục gốc lưu ứng dụng Web
        /// </summary>
        public static string ApplicationRootPath => _webHostEnvironment?.ContentRootPath ?? string.Empty;        

        /// <summary>
        /// Ghi dữ liệu vào session
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void SetSessionData(string key, object value)
        {
            try
            {
                string sValue = JsonConvert.SerializeObject(value);
                if (!string.IsNullOrEmpty(sValue))
                {
                    _httpContextAccessor?.HttpContext?.Session.SetString(key, sValue);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Đọc dữ liệu từ session
        /// Trả về một thể hiện mặc định nếu khóa không tồn tại hoặc không thể deserialize
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetSessionData<T>(string key) where T : class, new()
        {
            try
            {
                string sValue = _httpContextAccessor?.HttpContext?.Session.GetString(key) ?? string.Empty;
                if (!string.IsNullOrEmpty(sValue))
                {
                    var obj = JsonConvert.DeserializeObject<T>(sValue);
                    if (obj != null) return obj;
                }
            }
            catch
            {
            }
            return new T();
        }

        /// <summary>
        /// Lấy chuỗi giá trị của cấu hình trong appsettings.json
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetConfigValue(string name)
        {
            return _configuration?[name] ?? "";
        }

        /// <summary>
        /// Lấy đối tượng có kiểu là T trong phần cấu hình có tên là name trong appsettings.json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetConfigSection<T>(string name) where T : new()
        {
            var value = new T();
            _configuration?.GetSection(name).Bind(value);
            return value;
        }
        /// <summary>
        /// Số dòng cần hiển thị trên mỗi trang khi phân trang dữ liệu
        /// </summary>
        public static int PageSize => Convert.ToInt32(GetConfigValue("PageSize"));

        // Nested wrapper to allow syntax like: var x = new ApplicationContext.SessionData<T>(key);
        public class SessionData<T> where T : class, new()
        {
            private T _value;
            public SessionData(string key)
            {
                _value = ApplicationContext.GetSessionData<T>(key);
            }
            public static implicit operator T(SessionData<T> d) => d._value;
        }
    }
}
