using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JWT;
using JWT.Algorithms;
using JWT.Serializers;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Deestone.Models;
using Newtonsoft.Json;

namespace Deestone.Services
{
  public class JwtService
  {
    public string CreateToken(UserDataModel userData)
    {
      try
      {
        var payload = new Dictionary<string, object>
                {
                    { "exp", DateTimeOffset.UtcNow.AddHours(8).ToUnixTimeSeconds() },
                    { "data", userData}
                };

        string secret = Startup.GetTokenSecret();

        IJwtAlgorithm algorithm = new HMACSHA256Algorithm();
        IJsonSerializer serializer = new JsonNetSerializer();
        IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
        IJwtEncoder encoder = new JwtEncoder(algorithm, serializer, urlEncoder);

        var token = encoder.Encode(payload, secret);

        return token;
      }
      catch (Exception)
      {
        return "";
      }
    }

    public ResponseModel ValidateToken(string token)
    {
      string secret = Startup.GetTokenSecret();

      try
      {
        IJsonSerializer serializer = new JsonNetSerializer();
        IDateTimeProvider provider = new UtcDateTimeProvider();
        IJwtValidator validator = new JwtValidator(serializer, provider);
        IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
        IJwtDecoder decoder = new JwtDecoder(serializer, validator, urlEncoder);

        var json = decoder.Decode(token, secret, verify: true);

        PayloadModel payload = JsonConvert.DeserializeObject<PayloadModel>(json);

        return new ResponseModel
        {
          result = true,
          message = "Token valid.",
          data = payload.data
        };
      }
      catch (TokenExpiredException)
      {
        return new ResponseModel
        {
          result = false,
          message = "Token has expired",
          data = new UserDataModel { }
        };
      }
      catch (SignatureVerificationException)
      {
        return new ResponseModel
        {
          result = false,
          message = "Token has invalid signature",
          data = new UserDataModel { }
        };
      }
    }

    public string HashPassword(string password)
    {
      try
      {
        string hashed = Convert.ToBase64String(
            KeyDerivation.Pbkdf2(
                password: password,
                salt: Encoding.ASCII.GetBytes(Startup.GetTokenSecret()),
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 10000,
                numBytesRequested: 256 / 8
            )
        );

        return hashed;
      }
      catch (Exception)
      {
        return "";
      }

    }
  }
}
