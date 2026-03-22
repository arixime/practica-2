using System;

namespace WebAPI.Models
{
    public class Image
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public int Width {  get; set; }
        public int Height { get; set; }
        public string Type { get; set; }
        public DateTime AddedDate { get; set; }
        public string FilePath { get; set; }
    }
}
