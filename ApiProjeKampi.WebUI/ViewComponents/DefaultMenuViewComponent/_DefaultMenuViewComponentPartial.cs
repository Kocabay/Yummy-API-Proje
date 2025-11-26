using Microsoft.AspNetCore.Mvc;

namespace ApiProjeKampi.WebUI.ViewComponents.DefaultMenuViewComponent
{
    public class _DefaultMenuViewComponentPartial : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View();
        }
    }
}
