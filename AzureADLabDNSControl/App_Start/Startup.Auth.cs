﻿using System;
using Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System.Threading.Tasks;
using Microsoft.Owin.Security.Notifications;
using Microsoft.IdentityModel.Tokens;
using Lab.Common;
using Infra.Auth;

namespace AzureADLabDNSControl
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            var authProvider = new CookieAuthenticationProvider
            {
                OnResponseSignIn = ctx =>
                {
                    var task = Task.Run(async () => {
                        await AuthInit(ctx);
                    });
                    task.Wait();
                },
                OnValidateIdentity = ctx =>
                {
                    //good spot to troubleshoot nonces, etc...
                    return Task.FromResult(0);
                }
            };

            var cookieOptions = new CookieAuthenticationOptions
            {
                Provider = authProvider,
                CookieManager = new Microsoft.Owin.Host.SystemWeb.SystemWebChunkingCookieManager()
            };

            app.UseCookieAuthentication(cookieOptions);
            Utils.Authority = Settings.AdminAuthority;
            Utils.ClientId = Settings.LabAdminClientId;
            Utils.ClientSecret = Settings.LabAdminSecret;

            OpenIdConnectAuthenticationOptions LabAdminOptions = new OpenIdConnectAuthenticationOptions
            {
                AuthenticationType = CustomAuthType.LabAdmin,
                ClientId = Settings.LabAdminClientId,
                Authority = Settings.AdminAuthority,
                PostLogoutRedirectUri = "/",
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    AuthenticationFailed = AuthFailed,
                    AuthorizationCodeReceived = async (context) =>
                    {
                        await Utils.OnAuthorizationCodeReceived(context);
                        await AuthInit(context);
                    },
                    RedirectToIdentityProvider = (context) =>
                    {
                        string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;
                        context.ProtocolMessage.RedirectUri = appBaseUrl + "/";
                        context.ProtocolMessage.PostLogoutRedirectUri = appBaseUrl;
                        return Task.FromResult(0);
                    },
                },
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                }
            };
            app.UseOpenIdConnectAuthentication(LabAdminOptions);

            OpenIdConnectAuthenticationOptions LabUserOptions = new OpenIdConnectAuthenticationOptions
            {
                AuthenticationType = CustomAuthType.LabUser,
                ClientId = Settings.LabUserClientId,
                Authority = Settings.UserAuthority,
                PostLogoutRedirectUri = "/",
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    AuthenticationFailed = AuthFailed,
                    RedirectToIdentityProvider = (context) =>
                    {
                        string appBaseUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase;
                        context.ProtocolMessage.RedirectUri = appBaseUrl + "/";
                        context.ProtocolMessage.PostLogoutRedirectUri = appBaseUrl;
                        context.ProtocolMessage.Prompt = "login";

                        if (context.Request.Path.Value == "/account/refresh")
                        {
                            context.ProtocolMessage.LoginHint = context.Request.User.Identity.Name;
                            context.ProtocolMessage.Prompt = "none";
                        }
                        return Task.FromResult(0);
                    },
                },
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                }
            };
            app.UseOpenIdConnectAuthentication(LabUserOptions);

        }

        private Task AuthFailed(AuthenticationFailedNotification<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> arg)
        {
            arg.Response.Redirect(string.Format("/home?msg=", arg.Exception.Message));
            return Task.FromResult(0);
        }

        private static string EnsureTrailingSlash(string value)
        {
            if (value == null)
            {
                value = string.Empty;
            }

            if (!value.EndsWith("/", StringComparison.Ordinal))
            {
                return value + "/";
            }

            return value;
        }
    }
}
