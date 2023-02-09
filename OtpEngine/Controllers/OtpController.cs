using Microsoft.AspNetCore.Mvc;
using OtpVerfication.API;
using OtpVerfication.API.Utils;
using System.ComponentModel.DataAnnotations;

namespace OtpEngine.Controllers
{
    [ApiController]
    [Route("api/[controller]/[Action]")]
    public class OtpController : ControllerBase
    {
        private readonly IOtpVerfication otp;

        public record class User(string FullName)
        {
            public int Id { get; set; }
            public bool isVerify { get; set; }
        }

        public static List<User> users = new List<User>();

        public class Status
        {
            public string Message { get; set; }
            public bool? IsSuccess { get; set; } = false;
        }

        public class ResModel
        {
            public ResModel()
            {
                status = new Status();
            }
            public string code { get; set; }
            public DateTime Expiretime { get; set; }
            public Status status { get; set; }
        }

       

        public OtpController(IOtpVerfication otp)
        {
            this.otp = otp;
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            return Ok(users);
        }

        [HttpPost]
        public IActionResult CreateUser(User user)
        {
            ResModel res = new ResModel();
            user.isVerify = default;
            user.Id = int.Parse(Generator.RandomString(2, StringsOfLetters.Number));

            users.Add(user);

            var code = otp.Generate(user.Id.ToString(), expire: out DateTime expierDate);
            //This Code can be sent by Email or SMS
            res.code = code.Code;
            res.Expiretime = expierDate;
            res.status.IsSuccess = true;
            res.status.Message = "Successfully Generated";
            return Ok(res);
        }




        [HttpPost("{id}")]
        public IActionResult RefreshUser([FromRoute]int id)
        {
            var user = users.Where(u => u.Id == id).FirstOrDefault();
            if(user is null)
            {
                return NotFound($"user with Id:{id} not exist");
            }

            user.isVerify = false;

            var code = otp.Generate(user.Id.ToString(),expire: out DateTime expierDate);

            return Ok($"Your via is {code},\n will expire at : {expierDate}");

        }

        [HttpPost]
        public IActionResult VerifyUser([Range(0,99)] int userId, [Required]string code)
        {
            var one = users.Where(u => u.Id == userId).FirstOrDefault();
            if(one is null) { return NotFound($"user with Id:{userId} not exist"); }

            if(one.isVerify)
            {
                return BadRequest($"user with Id:{userId} is already verified");
            }

            if (otp.Scan(userId.ToString(), code))
            {
                one.isVerify = false;
                return Ok($"user with Id:{userId} successful confirmed his OTP code {code}");
            }

            return BadRequest($"user with Id:{userId} enter wrong code or expired");
        }
        
    }
}
