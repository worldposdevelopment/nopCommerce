
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Options;

using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using System.Dynamic;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using Nop.Data;
using Nop.Plugin.Misc.WebAPI.Domain;
using Nop.Services.Common;

namespace Nop.Plugin.Misc.WebAPI.Services
{
    public interface IUserService
    {
   //     String GenerateJWTToken(User user);
       Boolean SendSmsValidation(String PhoneNumber);
        Boolean ConfirmVerificationCode(String PhoneNumber, String PhoneCode);
    }

    public class UserValidationService : IUserService
    {
        //   List<User> users;

        private readonly IConfiguration _config;
        private readonly IRepository<PhoneActivation> _phoneActivationRepository;
    

        public UserValidationService(IConfiguration config, IRepository<PhoneActivation>  phoneActivationRepository)
        {
            _config = config;
            _phoneActivationRepository = phoneActivationRepository;
            
        }

        public bool ConfirmVerificationCode(string PhoneNumber, string PhoneCode)
        {
          var result =  _phoneActivationRepository.Table.Where(p => p.PhoneNo == PhoneNumber && p.ActivationCode == PhoneCode && p.Expiry >= DateTime.Now).OrderBy(p => p.DateTime).FirstOrDefault();
            if (result != null) {
                return true;
                    }
            else
                return false;
        }

        //public String GenerateJWTToken(User user)
        //{

        //    var key = Encoding.ASCII.GetBytes(_config["ConnectionStrings:hoopsstationDB"]);
        //    var jwtToken = new SecurityTokenDescriptor
        //    {
        //        Subject = new ClaimsIdentity(new Claim[] {
        //                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        //                     new Claim(ClaimTypes.Name, user.UserName)
        //                }),
        //        Expires = DateTime.UtcNow.AddDays(1),
        //        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        //    };
        //    var tokenHandler = new JwtSecurityTokenHandler();
        //    var token = tokenHandler.CreateToken(jwtToken);
        //    return tokenHandler.WriteToken(token);
        //}
        public Boolean SendSmsValidation(String phoneNumber)
        {

            
            Random _random = new Random();
            String randnum = _random.Next(0, 999999).ToString("D6");
            String message = "Your Hoopsstation PIN is " + randnum;
            try
            {
                dynamic digitalMedia = new ExpandoObject();
                digitalMedia.DigitalMedia = new ExpandoObject();
                digitalMedia.DigitalMedia.ClientID = _config["SmsCredentials:ClientID"]; 
                digitalMedia.DigitalMedia.Username = _config["SmsCredentials:Username"];
                digitalMedia.DigitalMedia.SEND = new[] {new { Media = "SMS", Priority = 1, Message = "RM0 "+ message, MessageType ="S",
                Destination = new []{new {MSISDN = phoneNumber, MsgID = "12e21321e1"    }}    }


            };
                string json = JsonConvert.SerializeObject(digitalMedia); Console.WriteLine(json);
                byte[] postBytes = Encoding.UTF8.GetBytes(json);
                var privatekey = _config["SmsCredentials:PrivateKey"];
                // step 1, calculate MD5 hash from input
                MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(json + privatekey);
                byte[] hash = md5.ComputeHash(inputBytes);
                // step 2, convert byte array to hex string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hash.Length; i++)
                {
                    sb.Append(hash[i].ToString(false ? "X2" : "x2"));
                }
                var md5key = sb.ToString();
                var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://gensuite11.genusis.com/api/dm/?Key=" + md5key);
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
                httpWebRequest.Method = "POST";
                httpWebRequest.ContentLength = postBytes.Length;
                using (Stream streamWriter = httpWebRequest.GetRequestStream())
                {
                    streamWriter.Write(postBytes, 0, postBytes.Length);
                    streamWriter.Flush(); streamWriter.Close();
                }
                var httpResponse = (HttpWebResponse)
                    httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd(); Console.WriteLine(result);

                }
                PhoneActivation phoneActivation = new PhoneActivation();
                phoneActivation.ActivationCode = randnum;
                phoneActivation.PhoneNo = phoneNumber;
                phoneActivation.DateTime = DateTime.Now;
                phoneActivation.Expiry = DateTime.Now.AddMinutes(1);
                 _phoneActivationRepository.Insert(phoneActivation);
               // _phoneActivationRepository.sa();
                return true;

            }
            catch (Exception ex){
                return false;
            }
        }
    }
}
