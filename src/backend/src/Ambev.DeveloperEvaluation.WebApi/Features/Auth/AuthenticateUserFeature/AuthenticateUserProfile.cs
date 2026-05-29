using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Auth.AuthenticateUser;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Auth.AuthenticateUserFeature;

/// <summary>
/// AutoMapper profile for authentication-related mappings
/// </summary>
public sealed class AuthenticateUserProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticateUserProfile"/> class
    /// </summary>
    public AuthenticateUserProfile()
    {
        CreateMap<User, AuthenticateUserResponse>()
            .ForMember(dest => dest.Token, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));

        // The controller maps the HTTP request to the application command and the
        // application result back to the HTTP response — both maps were missing,
        // which made every POST /api/auth fail with a 500.
        CreateMap<AuthenticateUserRequest, AuthenticateUserCommand>();
        CreateMap<AuthenticateUserResult, AuthenticateUserResponse>();
    }
}
