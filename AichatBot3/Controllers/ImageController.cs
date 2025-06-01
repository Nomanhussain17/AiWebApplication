using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AichatBot3.Service;

namespace AichatBot3.Controllers
{
    [Authorize(Roles = "User")]
    public class ImageController : Controller
    {
        private readonly ImageGenerationService _imageService;
        public ImageController(ImageGenerationService imageService)
        {
            _imageService = imageService;
        }
        public IActionResult ImageGenerate()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> ImageGenerate(string prompt, string model)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                ViewBag.Error = "Please enter a valid prompt.";
                return View();
            }
            try
            {
                var (imageUrl, generationTime) = await _imageService.GenerateImageAsync(prompt, model);
                ViewBag.ImageUrl = imageUrl;
                ViewBag.GenerationTime = generationTime.ToString("0.00");
                ViewBag.SelectedModel = model;
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"⚠️ {ex.Message}"; // Show actual error
            }
            return View();
        }
    }
}
