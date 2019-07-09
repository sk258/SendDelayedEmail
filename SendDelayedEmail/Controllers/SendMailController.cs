using MySql.Data.MySqlClient;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Threading;
using System.Web;
using System.Web.Http;

namespace SendDelayedEmail.Controllers
{
    public class SendDealyedEmailController : ApiController
    {
        private string message;
        MySqlConnection con;
        MySqlCommand cmd;

        public HttpResponseMessage Post()
        {
            string error_msg = "";
            string Emailid = HttpContext.Current.Request["Emailid"];
            string Email_check = this.validateEmailaddress(Emailid);
            if(Email_check != "ok")
            {
                error_msg += Email_check + "\n";
            }

            string Timeout = HttpContext.Current.Request["Timeout"];
            string time_check = this.validateTimeout(Timeout);
            if(time_check != "ok")
            {
                error_msg += time_check + "\n";
            }


            string Data = HttpContext.Current.Request["Data"];
            string data_check = this.validateData(Data);
            if(data_check != "ok")
            {
                error_msg += data_check + "\n";
            }
            if(error_msg != "")
            {
                var error_response = new HttpResponseMessage()
                {
                    Content = new StringContent("{\"error_messages\":\"" + error_msg + "\"}")
                };
                error_response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return error_response;
            }


            string FileName = HttpContext.Current.Request["FileName"];

            message = "A mail will be sent to you after "+(Timeout)+" seconds";
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
            Thread.Sleep(Int32.Parse(Timeout)*1000);
            MailMessage mail = new MailMessage();
            mail.To.Add(Emailid);
            if(FileName == "")
            {
                FileName = "profile.jpg";
            }
            try
            {
                File.WriteAllBytes(FileName, Convert.FromBase64String(Data));
            }
            catch(Exception e)
            {
                Console.Write("Issue when writing image file");
            }
            
            mail.From = new MailAddress("test@gmail.com");
            mail.Subject = "Email using Gmail";
            mail.Body = "Please find the attached file";
            mail.Attachments.Add(new Attachment(FileName));

            string htmlfilecontent = "<!DOCTYPE html><html lang='en'><head><meta charset='utf-8'> <meta name='viewport' content='width=device-width, initial-scale=1'> <link rel='stylesheet' href='https://maxcdn.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css'> <script src='https://ajax.googleapis.com/ajax/libs/jquery/3.4.1/jquery.min.js'></script> <script src='https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.14.7/umd/popper.min.js'></script> <script src='https://maxcdn.bootstrapcdn.com/bootstrap/4.3.1/js/bootstrap.min.js'></script></head><body> <div class='container'> <h2>Card Image</h2><div class='card' style='width:400px'> <img class='card-img-top' src='"+FileName+"' alt='Card image' style='width:100%'> <div class='card-body'> <h4 class='card-title'>"+FileName+"</h4> </div></div></div></body></html>";
            File.WriteAllText("sample.html", htmlfilecontent);


            mail.Attachments.Add(new Attachment("smaple.html"));
            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com"; //Or Your SMTP Server Address
            smtp.Port = 25;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential
            ("your_email_id", "password");//your Smtp Email ID and Password
            smtp.EnableSsl = true;
            try
            {
                smtp.Send(mail);
            }
            catch(Exception e)
            {
                Console.Write("Issue when sending mail"+e.Message);
            }
            
        }

        public void insertToDB(string Emailid, string Timeout, string Data, string FileName)
        {
            try
            {
                con = new MySqlConnection("Data Source=localhost;Database=apidb;User ID=root;Password=root");
                con.Open();
            }
            catch(Exception e)
            {
                Console.Write("Issue with sql connection:" + e.Message);
            }
           
            string str = "insert into formdata values ('" + Emailid + "'," + Timeout + ",'" + Data + "','"+ FileName+  "')";
            try
            {
                cmd = new MySqlCommand(str, con);
                cmd.ExecuteNonQuery();
            }
            catch(Exception e)
            {
                Console.Write("Issue with sql execution" + e.Message);
            }
            
        }

        public string validateEmailaddress(string Emailid)
        {
            try
            {
                var mail_test = new MailAddress(Emailid);
                return "ok";
            }
            catch(Exception e)
            {
                return "Email Issue:"+e.Message;
            }
           
        }

        public string validateTimeout(string Timeout)
        {
            try
            {
                var time_test = Int32.Parse(Timeout);
                if (time_test > 0)
                {
                    return "ok";
                }
                else
                {
                    return "negative timeout not possible";
                }
            }
            catch(Exception e)
            {
                return "Timeout Issue:"+e.Message;
            }
        }

        public string validateData(string Data)
        {
            try
            {
                var data_test = Convert.FromBase64String(Data);
                return "ok";
            }
            catch(Exception e)
            {
                return "Data Issue:"+e.Message;

            }

        }
    }
}

