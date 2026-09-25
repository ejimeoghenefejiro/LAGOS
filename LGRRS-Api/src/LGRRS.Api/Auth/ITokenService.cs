namespace LGRRS.Api.Auth;

public interface ITokenService
{
    string IssueToken(string subjectId, string role, string displayName);
}
