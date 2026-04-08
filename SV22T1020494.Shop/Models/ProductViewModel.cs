namespace SV22T1020494.Shop.Models
{
    /// <summary>
    /// Đây là l?p nh? dùng cho hi?n th? danh sách/chi ti?t nh? mà không ph? thu?c tr?c tiếp
    /// vào model d? li?u l?n t? project Models.
    /// </summary>
    public class ProductViewModel
    {
        /// <summary> M? s?n ph?m. </summary>
        public int ProductID { get; set; }

        /// <summary> Tên s?n ph?m. </summary>
        public string? ProductName { get; set; }

        /// <summary> ?nh đ?i di?n (tên file ho?c đư?ng d?n tương đ?i). </summary>
        public string? Photo { get; set; }

        /// <summary> Giá bán. </summary>
        public decimal Price { get; set; }

        /// <summary> Cho bi?t s?n ph?m đang bán hay không. </summary>
        public bool IsSelling { get; set; }
    }
}
