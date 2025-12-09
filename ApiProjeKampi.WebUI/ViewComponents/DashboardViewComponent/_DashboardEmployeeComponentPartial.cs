using Microsoft.AspNetCore.Mvc;

namespace ApiProjeKampi.WebUI.ViewComponents.DashboardViewComponent
{
    public class _DashboardEmployeeComponentPartial : ViewComponent
    {
        private readonly IHttpClientFactory _httpClient;

        public _DashboardEmployeeComponentPartial(IHttpClientFactory httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View();
        }
    }
}
