using AutoMapper;
using HRMS.Application.DTOs;
using HRMS.Domain.Entities;
using HRMS.Domain.Interfaces;
using MediatR;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HRMS.Application.Features.Auth.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
    {
        private readonly IMapper _mapper;
        private readonly IUserRepository _userRepository;

        public LoginCommandHandler(IMapper mapper, IUserRepository userRepository)
        {
            _mapper = mapper;
            _userRepository = userRepository;
        }

        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            // 🔹 Lookup user from DB
            var user = await _userRepository.GetByEmailAsync(request.Email);

            if (user == null || user.PasswordHash != request.Password)
            {
                return new LoginResponse { IsSuccess = false, Message = "Invalid credentials" };
            }

            // 🔹 Generate JWT
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("ThisIsASuperStrongJWTSecretKey_ChangeMe_1234567890!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            // 🔹 Map to DTO
            var userDto = _mapper.Map<UserDto>(user);

            return new LoginResponse
            {
                IsSuccess = true,
                Message = "Login Successful",
                Token = jwt,
                User = userDto
            };
        }
    }
}
