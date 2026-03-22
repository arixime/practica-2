using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using WebAPI.Models;
using WebAPI.Data;
using System.IO;
using ModelImage = WebAPI.Models.Image;
using System.Security.Cryptography.X509Certificates;

namespace WebAPI.Controllers
{
    [Route("api/image")]
    [ApiController]
    public class ImagesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ImagesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<
            ModelImage>>> GetImages()
        {
            return await _context.Images.ToListAsync();
        }

        [HttpPost("add")]
        public async Task<ActionResult<ModelImage>> AddImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не выбран");

            var uploadsFolder = Path.Combine(_env.ContentRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            using (var image = await SixLabors.ImageSharp.Image.LoadAsync(filePath))
            {
                var width = image.Width;
                var height = image.Height;

                var imageEntity = new ModelImage
                {
                    Name = file.FileName,
                    Size = file.Length,
                    Width = width,
                    Height = height,
                    Type = Path.GetExtension(file.FileName).TrimStart('.'),
                    AddedDate = DateTime.UtcNow,
                    FilePath = filePath
                };

                _context.Images.Add(imageEntity);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetImages), new { id = imageEntity.Id }, imageEntity);
            }
        }

        public class ResizeRequest
        {
            public string FilePath { get; set; }
            public int NewWidth { get; set; }
            public int NewHeight { get; set; }
        }

        [HttpPut("change/size")]
        public async Task<IActionResult> ChangeSize([FromBody] ResizeRequest request)
        {
            if (!System.IO.File.Exists(request.FilePath))
                return NotFound("Файл не найден");

            using (var image = await SixLabors.ImageSharp.Image.LoadAsync(request.FilePath))
            {
                image.Mutate(x => x.Resize(request.NewWidth, request.NewHeight));

                var directory = Path.GetDirectoryName(request.FilePath);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(request.FilePath);
                var extension = Path.GetExtension(request.FilePath);

                var newFileName = $"{fileNameWithoutExt}_resized{extension}";
                var newFilePath = Path.Combine(directory, newFileName);

                await image.SaveAsync(newFilePath);

                var newImage = new ModelImage
                {
                    Name = newFileName,
                    Size = new FileInfo(newFilePath).Length,
                    Width = request.NewWidth,
                    Height = request.NewHeight,
                    Type = extension.TrimStart('.'),
                    AddedDate = DateTime.UtcNow,
                    FilePath = newFilePath
                };

                _context.Images.Add(newImage);
                await _context.SaveChangesAsync();

                return Ok(newImage);
            }
        }

        public class RotateRequest
        {
            public string FilePath { get; set; }
            public float Angle { get; set; }
        }

        [HttpPut("change/rotate")]
        public async Task<IActionResult> Rotate([FromBody] RotateRequest request)
        {
            if (!System.IO.File.Exists(request.FilePath))
                return NotFound("Файл не найден");

            using (var ModelImage = await SixLabors.ImageSharp.Image.LoadAsync(request.FilePath))
            {
                ModelImage.Mutate(x => x.Rotate(request.Angle));

                var directory = Path.GetDirectoryName(request.FilePath);
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(request.FilePath);
                var extension = Path.GetExtension(request.FilePath);
                var newFileName = $"{fileNameWithoutExt}_rotated{extension}";
                var newFilePath = Path.Combine(directory, newFileName);

                await ModelImage.SaveAsync(newFilePath);

                var newImage = new ModelImage
                {
                    Name = newFileName,
                    Size = new FileInfo(newFilePath).Length,
                    Width = ModelImage.Width,
                    Height = ModelImage.Height,
                    Type = extension.TrimStart('.'),
                    AddedDate = DateTime.UtcNow,
                    FilePath = newFilePath
                };

                _context.Images.Add(newImage);
                await _context.SaveChangesAsync();

                return Ok(newImage);
            }
        }
    }
}
