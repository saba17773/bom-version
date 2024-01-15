using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deestone.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Deestone.Services
{
  public class EmailService
  {
    public ResponseModel SendEmail(string subject, string htmlBody, List<string> mailTo, string replyTo = "")
    {
      try
      {
        var message = new MimeMessage();
        var bodyBuilder = new BodyBuilder();

        message.From.Add(new MailboxAddress("SYSTEM", "ea_devteam@deestone.com"));

        if (mailTo.Count > 0)
        {
          foreach (var mail in mailTo)
          {
            message.To.Add(new MailboxAddress(mail, mail));
          }
        }
        else
        {
          throw new Exception("No email to send to.");
        }

        if (replyTo != "")
        {
          message.ReplyTo.Add(new MailboxAddress(replyTo, replyTo));
        }

        message.Subject = subject;
        bodyBuilder.HtmlBody = htmlBody;
        message.Body = bodyBuilder.ToMessageBody();

        var client = new SmtpClient();

        client.ServerCertificateValidationCallback = (s, c, h, e) => true;
        client.Connect("20.20.20.3", 465, SecureSocketOptions.SslOnConnect);
        client.Authenticate("ea_devteam@deestone.com", "$devT$78420");
        client.Send(message);
        client.Disconnect(true);

        return new ResponseModel
        {
          result = true,
          message = "Send mail success."
        };
      }
      catch (Exception ex)
      {
        return new ResponseModel
        {
          result = false,
          message = ex.Message
        };
      }

    }
  }
}
