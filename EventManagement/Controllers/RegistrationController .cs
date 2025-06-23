using EventManagement.Models;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using ClosedXML.Excel;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using iText.IO.Image;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout.Properties;
using iText.Layout.Borders;

namespace EventManagement.Controllers
{
    public class RegistrationController : Controller
    {
        private readonly string _connectionString;
        private readonly IWebHostEnvironment _env;

        public RegistrationController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            _env = env;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult RegisterAjax(string ContactNo, string FirstName, string LastName, int GuestCount)
        {
            try
            {
                string uniqueCode = $"REG-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";

                using (var con = new NpgsqlConnection(_connectionString))
                {
                    con.Open();
                    string checkQuery = "SELECT COUNT(*) FROM Registration WHERE ContactNo = @ContactNo";
                    using (var checkCmd = new NpgsqlCommand(checkQuery, con))
                    {
                        checkCmd.Parameters.AddWithValue("@ContactNo", ContactNo);
                        long count = (long)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            return Json(new { success = false, message = "This contact number is already registered." });
                        }
                    }

                    string insertQuery = @"INSERT INTO Registration 
                                (FirstName, LastName, GuestCount, ContactNo, UniqueCode, CreatedAt) 
                                VALUES (@FirstName, @LastName, @GuestCount, @ContactNo, @UniqueCode, @CreatedAt)";

                    using (var cmd = new NpgsqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.AddWithValue("@FirstName", FirstName);
                        cmd.Parameters.AddWithValue("@LastName", LastName);
                        cmd.Parameters.AddWithValue("@GuestCount", GuestCount);
                        cmd.Parameters.AddWithValue("@ContactNo", ContactNo);
                        cmd.Parameters.AddWithValue("@UniqueCode", uniqueCode);
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true, message = "Registration successful!", code = uniqueCode });
            }
            catch
            {
                return Json(new { success = false, message = "Server error occurred." });
            }
        }

        public IActionResult DownloadTicket(string code, string firstName, string lastName, string contactNo, int guestCount)
        {
            try
            {


                byte[] pdfBytes;

                using (var stream = new MemoryStream())
                {
                    var writer = new PdfWriter(stream);
                    var pdf = new PdfDocument(writer);
                    var doc = new Document(pdf);
                    var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                    var normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                    //string imagePath = Path.Combine(_env.WebRootPath, "Image", "UniqueItLogo.png"); // "Image" not "images"
                    //ImageData imageData = ImageDataFactory.Create(imagePath);
                    //Image img = new Image(imageData);
                    //img.SetWidth(180);
                    //img.SetHeight(50);
                    //img.SetHorizontalAlignment(iText.Layout.Properties.HorizontalAlignment.CENTER);
                    //img.SetMarginBottom(10);
                    //doc.Add(img);

                    string imagePath1 = Path.Combine(_env.WebRootPath, "Image", "UniqueItLogo.png");
                    string imagePath2 = Path.Combine(_env.WebRootPath, "Image", "RoyalRajwadilogoremovebg.png");

                    ImageData imageData1 = ImageDataFactory.Create(imagePath1);
                    ImageData imageData2 = ImageDataFactory.Create(imagePath2);

                    Image img1 = new Image(imageData1).SetWidth(180).SetHeight(50);
                    Image img2 = new Image(imageData2).SetWidth(120).SetHeight(70);

                    // Create a table with 2 columns, using full width
                    Table imageRow = new Table(2).UseAllAvailableWidth();

                    // Add both images to cells, center-aligned and without borders
                    imageRow.AddCell(
                        new Cell().Add(img1)
                                  .SetBorder(Border.NO_BORDER)
                                  .SetTextAlignment(TextAlignment.CENTER));

                    imageRow.AddCell(
                        new Cell().Add(img2)
                                  .SetBorder(Border.NO_BORDER)
                                  .SetTextAlignment(TextAlignment.CENTER));

                    // Optional: add bottom margin for spacing after the images
                    imageRow.SetMarginBottom(10);

                    // Add the image row to the PDF document
                    doc.Add(imageRow);


                    LineSeparator underline = new LineSeparator(new SolidLine());
                    underline.SetWidth(UnitValue.CreatePercentValue(100));
                    underline.SetMarginBottom(15);

                    doc.Add(underline);
                    doc.Add(new Paragraph("Event Ticket Confirmation").SetFont(bold).SetFontSize(18));
                    doc.Add(new Paragraph($"Unique Code: {code}").SetFont(bold));
                    doc.Add(new Paragraph($"Name: {firstName} {lastName}").SetFont(normal));
                    doc.Add(new Paragraph($"Contact No: {contactNo}").SetFont(normal));
                    doc.Add(new Paragraph($"Guest Count: {guestCount}").SetFont(normal));
                    doc.Add(new Paragraph($"Registration Date: {DateTime.Now}").SetFont(normal));

                    doc.Close();

                    pdfBytes = stream.ToArray();
                }

                return File(pdfBytes, "application/pdf", $"EventTicket_{code}.pdf");
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }
        }


        public IActionResult WantToDownLoadExcel(string passcode)
        {
            if (passcode != "#manish&&*9090")
            {
                return RedirectToAction("Index");
            }
            try
            {
                DataTable dataTable = new DataTable();

                using (var con = new NpgsqlConnection(_connectionString))
                {
                    con.Open();
                    string query = "SELECT FirstName, LastName, GuestCount, ContactNo, UniqueCode, CreatedAt FROM Registration ORDER BY CreatedAt DESC";

                    using (var cmd = new NpgsqlCommand(query, con))
                    using (var reader = cmd.ExecuteReader())
                    {
                        dataTable.Load(reader);
                    }
                }

                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add("Registrations");
                    worksheet.Cell(1, 1).InsertTable(dataTable);

                    using (var stream = new MemoryStream())
                    {
                        workbook.SaveAs(stream);
                        stream.Seek(0, SeekOrigin.Begin);

                        return File(stream.ToArray(),
                                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                    $"Event_Registrations_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                    }
                }
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
            }
        }


        //[HttpPost]
        //public IActionResult Registration([FromBody] RegistrationModel model)
        //{
        //    try
        //    {
        //        // Generate unique code
        //        string uniqueCode = $"REG-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";

        //        // Check for duplicate contact number
        //        using (var con = new NpgsqlConnection(_connectionString))
        //        {
        //            con.Open();
        //            string checkQuery = "SELECT COUNT(*) FROM Registration WHERE ContactNo = @ContactNo";
        //            using (var checkCmd = new NpgsqlCommand(checkQuery, con))
        //            {
        //                checkCmd.Parameters.AddWithValue("@ContactNo", model.ContactNo);
        //                long count = (long)checkCmd.ExecuteScalar();
        //                if (count > 0)
        //                {
        //                    ModelState.AddModelError("ContactNo", "This contact number is already registered.");
        //                    return View("Index", model);
        //                }
        //            }

        //            // Insert new record
        //            string insertQuery = @"INSERT INTO Registration 
        //                                (FirstName, LastName, GuestCount, ContactNo, UniqueCode, CreatedAt) 
        //                                VALUES (@FirstName, @LastName, @GuestCount, @ContactNo, @UniqueCode, @CreatedAt)";

        //            using (var cmd = new NpgsqlCommand(insertQuery, con))
        //            {
        //                cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
        //                cmd.Parameters.AddWithValue("@LastName", model.LastName);
        //                cmd.Parameters.AddWithValue("@GuestCount", model.GuestCount);
        //                //cmd.Parameters.AddWithValue("@EventDate", model.EventDate);
        //                cmd.Parameters.AddWithValue("@ContactNo", model.ContactNo);
        //                //cmd.Parameters.AddWithValue("@PeopleNames", model.PeopleNames);
        //                cmd.Parameters.AddWithValue("@UniqueCode", uniqueCode);
        //                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
        //                cmd.ExecuteNonQuery();
        //            }
        //        }

        //        byte[] pdfBytes;
        //        using (var stream = new MemoryStream())
        //        {
        //            var writer = new PdfWriter(stream);
        //            var pdf = new PdfDocument(writer);
        //            var doc = new Document(pdf);
        //            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        //            var normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

        //            doc.Add(new Paragraph("🎟 Event Ticket Confirmation").SetFont(bold).SetFontSize(18));
        //            doc.Add(new Paragraph($"Name: {model.FirstName} {model.LastName}").SetFont(normal));
        //            doc.Add(new Paragraph($"Contact No: {model.ContactNo}").SetFont(normal));
        //            doc.Add(new Paragraph($"Guest Count: {model.GuestCount}").SetFont(normal));
        //            doc.Add(new Paragraph($"Unique Code: {uniqueCode}").SetFont(bold));
        //            doc.Close();

        //            pdfBytes = stream.ToArray();
        //        }

        //        TempData["Message"] = "Registration SuccessFully";
        //        TempData.Keep("Message");
        //        return File(pdfBytes, "application/pdf", $"EventTicket_{uniqueCode}.pdf");

        //    }
        //    catch (Exception ex)
        //    {
        //        return RedirectToAction("Index");
        //    }
        //}

        //public IActionResult DownloadAndRefresh()
        //{
        //    if (TempData["UniqueCode"] == null)
        //        return RedirectToAction("Index");

        //    var uniqueCode = TempData["UniqueCode"].ToString();
        //    var name = TempData["UserName"]?.ToString();
        //    var contact = TempData["ContactNo"]?.ToString();
        //    var date = TempData["EventDate"]?.ToString();
        //    var guests = TempData["GuestCount"]?.ToString();
        //    var people = TempData["PeopleNames"]?.ToString();


        //}

        //[HttpPost]
        //public IActionResult Registration(RegistrationModel model)
        //{
        //    try
        //    {
        //        //var now = DateTime.Now.TimeOfDay;
        //        //var startRestriction = new TimeSpan(17, 0, 0); 
        //        //var endRestriction = new TimeSpan(8, 0, 0);    

        //        //if (now >= startRestriction || now < endRestriction)
        //        //{
        //        //    TempData["ErrorMessage"] = "Registration is only allowed between 8 AM and 5 PM.";
        //        //    return RedirectToAction("Index");
        //        //}

        //        string uniqueCode = $"REG-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 5).ToUpper()}";


        //        using (NpgsqlConnection con = new NpgsqlConnection(_connectionString))
        //        {
        //            con.Open();

        //            string checkQuery = "SELECT COUNT(*) FROM Registration WHERE ContactNo = @ContactNo";
        //            using (NpgsqlCommand checkCmd = new NpgsqlCommand(checkQuery, con))
        //            {
        //                checkCmd.Parameters.AddWithValue("@ContactNo", model.ContactNo);

        //                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

        //                if (count > 0)
        //                {
        //                    TempData["ErrorMessage"] = "This contact number has already been registered.";
        //                    return RedirectToAction("Index");
        //                }
        //            }

        //            string insertQuery = @"INSERT INTO Registration 
        //                  (FirstName, LastName, GuestCount, EventDate, ContactNo, PeopleNames, UniqueCode, CreatedAt) 
        //                  VALUES (@FirstName, @LastName, @GuestCount, @EventDate, @ContactNo, @PeopleNames, @UniqueCode, @CreatedAt)";

        //            using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, con))
        //            {
        //                cmd.Parameters.AddWithValue("@FirstName", model.FirstName);
        //                cmd.Parameters.AddWithValue("@LastName", model.LastName);
        //                cmd.Parameters.AddWithValue("@GuestCount", model.GuestCount);
        //                cmd.Parameters.AddWithValue("@EventDate", model.EventDate.Date);
        //                cmd.Parameters.AddWithValue("@ContactNo", model.ContactNo);
        //                cmd.Parameters.AddWithValue("@PeopleNames", model.PeopleNames);
        //                cmd.Parameters.AddWithValue("@UniqueCode", uniqueCode);
        //                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

        //                cmd.ExecuteNonQuery();
        //            }

        //            con.Close();
        //        }




        //        byte[] pdfBytes;

        //        using (var stream = new MemoryStream())
        //        {
        //            var writer = new PdfWriter(stream);
        //            var pdf = new PdfDocument(writer);
        //            var doc = new Document(pdf);

        //            PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        //            PdfFont regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

        //            doc.Add(new Paragraph("Event Registration Confirmation")
        //                .SetFont(boldFont)
        //                .SetFontSize(18));

        //            doc.Add(new Paragraph($"Name: {model.FirstName} {model.LastName}").SetFont(regularFont));
        //            doc.Add(new Paragraph($"Guest Count: {model.GuestCount}").SetFont(regularFont));
        //            doc.Add(new Paragraph($"Date: {model.EventDate:dd-MM-yyyy}").SetFont(regularFont));
        //            doc.Add(new Paragraph($"Contact No: {model.ContactNo}").SetFont(boldFont));
        //            doc.Add(new Paragraph($"PeopleName: {model.PeopleNames}").SetFont(regularFont));
        //            doc.Add(new Paragraph($"Unique Code: {uniqueCode}").SetFont(boldFont));

        //            doc.Close();

        //            pdfBytes = stream.ToArray();
        //        }
        //        TempData["Message"] = "Registration SuccessFully";
        //        return File(pdfBytes, "application/pdf", $"EventTicket_{uniqueCode}.pdf");
        //    }
        //    catch (Exception ex)
        //    {
        //        return RedirectToAction("Index");
        //    }
        //}

    }
}