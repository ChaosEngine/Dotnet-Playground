using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DotnetPlayground.Controllers
{
    //create brand new endpoint /enterprise that accepts a query string parameter named state
    [Route("[controller]")]
    public class EnterpriseController : Controller
    {
        private ILogger<EnterpriseController> _logger;

        public EnterpriseController(ILogger<EnterpriseController> logger)
        {
            _logger = logger;
        }

        private string AppendQueryParameter(string state, string paramName, string paramValue)
        {
            // 1. Start with your full URL
            var uriBuilder = new UriBuilder(state);

            // 2. Parse the existing query string
            // uriBuilder.Query will be "?type=json&page=2"
            NameValueCollection queryParams = HttpUtility.ParseQueryString(uriBuilder.Query);

            // 3. Add your new parameter
            // If the parameter might already exist and you want to update it,
            queryParams.Set(paramName.Trim(), paramValue);

            // 4. Set the builder's query to the modified string
            // queryParams.ToString() will be "type=json&page=2&code=newValue"
            uriBuilder.Query = queryParams.ToString();

            // The final URL is now correct
            // https://api.example.com/v1/data?type=json&page=2&code=newValue
            string appendedUrl = uriBuilder.ToString();

            return appendedUrl;
        }

        private string GenerateSimpleJwtToken(string orgShortName, string code)
        {
            //return sample jwt token with some subject, some issuer, and expiration date 1 hour from now
            var jwtHeader = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{\"alg\":\"HS256\",\"typ\":\"JWT\"}}"));
            var jwtPayload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{{\"sub\":\"{orgShortName}\",\"name\": \"John Doe\",\"iss\":\"issuer.auth.example.com\",\"aud\":\"audience.auth.example.com\",\"exp\":\"{DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()}\",\"iat\":\"{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}\"}}"));
            var jwtSignature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("secret-key-must-be-at-least-256-bits-long-long-long-long-long-long"));

            return $"{jwtHeader}.{jwtPayload}.{jwtSignature}";
        }

        private string GenerateSampleJwtTokenV2(string orgShortName, string code)
        {
            Claim[] claims = [new Claim("Org", orgShortName)];
            //base64 decode code and add as another claim
            try
            {
                var decodedCode = Encoding.UTF8.GetString(Convert.FromBase64String(code));
                if (!string.IsNullOrEmpty(decodedCode) && decodedCode.Contains('.'))
                    claims = new List<Claim>(claims) { new Claim("Name", decodedCode.Split('.')[0].Trim()) }.ToArray();
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it)
                _logger.LogWarning(ex, "Failed to decode base64 code: {code}", code);
            }

            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("secret-key-must-be-at-least-256-bits-long-long-long-long-long-long"));
            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.RsaSha256);

            var tokeOptions = new JwtSecurityToken(
                issuer: "issuer.auth.example.com",
                audience: "audience.auth.example.com",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: signinCredentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokeOptions);

            return tokenString;
        }

        [HttpGet("v1/authint/bi/{orgShortName}")]
        [Authorize()]
        public IActionResult Get([Required]string orgShortName, [FromQuery] string authcodecallback)
        {
            return View("Index");
        }

        [HttpPost("v1/authint/bi/{orgShortName}")]
        [Authorize()]
        public IActionResult RedirectWithCode([Required]string orgShortName, /* [FromQuery]  */string authcodecallback)
        {
            if (string.IsNullOrEmpty(orgShortName))
                return BadRequest("orgShortName parameter is missing");

            if (string.IsNullOrEmpty(authcodecallback))
                return BadRequest("authcodecallback parameter is missing");
            if (!Uri.TryCreate(authcodecallback, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                return BadRequest("authcodecallback must be a valid absolute URL with https scheme (https://...)");

            var redirectUrl = AppendQueryParameter(authcodecallback, "code", Guid.NewGuid().ToString());

            _logger.LogWarning("Generating redirect URL for orgShortName: {orgShortName}, authcodecallback: {authcodecallback}, redirectUrl: {redirectUrl}", orgShortName, authcodecallback, redirectUrl);

            // return Content($"OrgShortName: {orgShortName}, code: {redirectUrl}");
            return Redirect(redirectUrl);
        }

        [HttpGet("v1/authint/authtoken/{orgShortName}")]
        public IActionResult GenerateToken([Required]string orgShortName, [FromQuery] string code)
        {
            if (string.IsNullOrEmpty(orgShortName))
			{
				_logger.LogWarning("orgShortName parameter is missing in GenerateToken request");
                return BadRequest("orgShortName parameter is missing");
			}
            if (!string.IsNullOrEmpty(code))
            {
                //generate sample jwt token
                // var token = GenerateSampleJwtToken(orgShortName, code);
                var token = GenerateSampleJwtTokenV2(orgShortName, code);

                _logger.LogWarning("Generating JWT token for orgShortName: {orgShortName}, code: {code}, token: {token}",
                    orgShortName, code, token);

                return Json(new { token });
            }
            else
            {
                try
                {
                    //obtain bearer token from Authorization header
                    var authHeader = Request.Headers.ContainsKey("Authorization") ? Request.Headers["Authorization"].ToString() : null;
                    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                    {
                        _logger.LogWarning("Missing or invalid Authorization header in GenerateToken request");
                        return Unauthorized("Missing or invalid Authorization header");
                    }
                    var bearerToken = authHeader.Substring("Bearer ".Length).Trim();
                    //parse jwt token
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(bearerToken);
                    //get name from claims
                    var nameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "Name");
                    var username = nameClaim != null ? nameClaim.Value : "Unknown";

                    var userPlusRand = username + '.' + Guid.NewGuid().ToString();
                    code = Convert.ToBase64String(Encoding.UTF8.GetBytes(userPlusRand));
                    var token = GenerateSampleJwtTokenV2(orgShortName, code);

                    return Json(new { token });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to refresh JWT token");
                    return BadRequest("orgShortName parameter is missing");
                }
            }
        }
    }
}
