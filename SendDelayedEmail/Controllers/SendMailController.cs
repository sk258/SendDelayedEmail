using System;
using System.Threading;
using System.Net.Http;
using System.Web.Http;
using System.Net.Mail;
using System.Web;
using System.IO;
using MySql.Data.MySqlClient;
using System.Net.Http.Headers;

namespace SendDelayedEmail.Controllers
{
    public class SendDealyedEmailController : ApiController
    {
        private string message;
        MySqlConnection con;
        MySqlCommand cmd;

        public HttpResponseMessage Post()
        {
            string Emailid = HttpContext.Current.Request["Emailid"];
            string Timeout = HttpContext.Current.Request["Timeout"];
            string Data = HttpContext.Current.Request["Data"];
            string FileName = HttpContext.Current.Request["FileName"];

            message = "A mail will be sent to you after "+Timeout+" seconds";
            ThreadStart mailThread = new ThreadStart(() => SendMail(Emailid, Timeout, Data, FileName));
            ThreadStart dbThread = new ThreadStart(() => insertToDB(Emailid, Timeout, Data, FileName));

            Thread mailChild = new Thread(mailThread);
            Thread dbChild = new Thread(dbThread);

            dbChild.Start();
            mailChild.Start();

            var response = new HttpResponseMessage()
            {
                Content = new StringContent("{\"message\":\""+message+"\"}")
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return response;
        }

        public void SendMail(string Emailid, string Timeout, string Data, string FileName)
        {
            Thread.Sleep(Int32.Parse(Timeout)*10);
            MailMessage mail = new MailMessage();
            mail.To.Add(Emailid);
            File.WriteAllBytes(FileName, Convert.FromBase64String(Data));

            mail.From = new MailAddress("test@gmail.com");
            mail.Subject = "Email using Gmail";
            mail.Body = message;
            mail.Attachments.Add(new Attachment(FileName));


            string htmlfilecontent = "<!DOCTYPE html><html lang='en'><head><meta charset='utf-8'> <meta name='viewport' content='width=device-width, initial-scale=1'> <link rel='stylesheet' href='https://maxcdn.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css'> <script src='https://ajax.googleapis.com/ajax/libs/jquery/3.4.1/jquery.min.js'></script> <script src='https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.14.7/umd/popper.min.js'></script> <script src='https://maxcdn.bootstrapcdn.com/bootstrap/4.3.1/js/bootstrap.min.js'></script></head><body> <div class='container'> <h2>Card Image</h2><div class='card' style='width:400px'> <img class='card-img-top' src='"+FileName+"' alt='Card image' style='width:100%'> <div class='card-body'> <h4 class='card-title'>"+FileName+"</h4> </div></div></div></body></html>";
            File.WriteAllText("smaple.html", htmlfilecontent);


            mail.Attachments.Add(new Attachment("smaple.html"));
            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com"; //Or Your SMTP Server Address
            smtp.Port = 25;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential
            ("YouEmailId", "password");//your Smtp Email ID and Password
            smtp.EnableSsl = true;
            smtp.Send(mail);
        }

        public void insertToDB(string Emailid, string Timeout, string Data, string FileName)
        {
            con = new MySqlConnection("Data Source=localhost;Database=apidb;User ID=root;Password=root");
            con.Open();
            string str = "insert into formdata values ('" + Emailid + "'," + Timeout + ",'" + Data + "','"+ FileName+  "')";
            cmd = new MySqlCommand(str, con);
            cmd.ExecuteNonQuery();

        }
    }
}

