using ApiProjeKampi.WebApi.Context;
using ApiProjeKampi.WebApi.Dtos.ReservationDtos;
using ApiProjeKampi.WebApi.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace ApiProjeKampi.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationsController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly IMapper _mapper;
        public ReservationsController(ApiContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // Tüm rezervasyonları listeler
        [HttpGet]
        public IActionResult ReservationList()
        {
            var values = _context.Reservations.ToList();
            return Ok(values);
        }

        // Yeni rezervasyon oluşturur
        [HttpPost]
        public IActionResult CreateReservation(CreateReservationDto createReservationDto)
        {
            var value = _mapper.Map<Reservation>(createReservationDto);
            _context.Reservations.Add(value);
            _context.SaveChanges();
            return Ok("Kategori ekleme işlemi başarılı");
        }

        // ID'ye göre rezervasyon siler
        [HttpDelete]
        public IActionResult DeleteReservation(int id)
        {
            var value = _context.Reservations.Find(id);
            _context.Reservations.Remove(value);
            _context.SaveChanges();
            return Ok("Kategori silme işlemi başarılı");
        }

        // ID'ye göre rezervasyon getirir
        [HttpGet("GetReservation")]
        public IActionResult GetReservation(int id)
        {
            var value = _context.Reservations.Find(id);
            return Ok(value);
        }

        // Rezervasyonu günceller
        [HttpPut]
        public IActionResult UpdateReservation(UpdateReservationDto updateReservationDto)
        {
            var value = _mapper.Map<Reservation>(updateReservationDto);
            _context.Reservations.Update(value);
            _context.SaveChanges();
            return Ok("Kategori güncelleme işlemi başarılı");
        }

        // Toplam rezervasyon sayısını getirir
        [HttpGet("GetTotalReservationCount")]
        public IActionResult GetTotalReservationCount()
        {
            var value = _context.Reservations.Count();
            return Ok(value);
        }

        // Tüm rezervasyonlardaki toplam kişi sayısını getirir
        [HttpGet("GetTotalCustomerCount")]
        public IActionResult GetTotalCustomerCount()
        {
            var value = _context.Reservations.Sum(x => x.CountOfPeople);
            return Ok(value);
        }

        // Onay bekleyen rezervasyon sayısını getirir
        [HttpGet("GetPendingReservations")]
        public IActionResult GetPendingReservations()
        {
            var value = _context.Reservations.Where(x => x.ReservationStatus == "Onay Bekliyor").Count();
            return Ok(value);
        }

        // Onaylanan rezervasyon sayısını getirir
        [HttpGet("GetApprovedReservations")]
        public IActionResult GetApprovedReservations()
        {
            var value = _context.Reservations.Where(x => x.ReservationStatus == "Onaylandi").Count();
            return Ok(value);
        }

        [HttpGet("GetReservationStats")]
        public IActionResult GetReservationStats()
        {
            DateTime today = DateTime.Today;
            DateTime fourMonthsAgo = today.AddMonths(-3);

            // 1. SQL tarafında sadece gruplama ve veri çekme
            var rawData = _context.Reservations
                .Where(r => r.ReservationDate >= fourMonthsAgo)
                .GroupBy(r => new { r.ReservationDate.Year, r.ReservationDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Approved = g.Count(x => x.ReservationStatus == "Onaylandi"),
                    Pending = g.Count(x => x.ReservationStatus == "Onay Bekliyor"),
                    Canceled = g.Count(x => x.ReservationStatus == "Iptal Edildi")
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList(); // Burada SQL biter, veriler RAM’e alınır

            // 2. Bellekte DTO'ya mapleme + tarih formatlama
            var result = rawData.Select(x => new ReservationChartDto
            {
                Month = new DateTime(x.Year, x.Month, 1).ToString("MMM yyyy"),
                Approved = x.Approved,
                Pending = x.Pending,
                Canceled = x.Canceled
            }).ToList();

            return Ok(result);
        }
    }
}
