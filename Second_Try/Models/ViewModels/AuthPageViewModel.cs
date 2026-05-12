using Second_Try.Models.ViewModels;

namespace Second_Try.Models.ViewModels
{
    public class AuthPageViewModel
    {
        public LoginViewModel Login { get; set; } = new LoginViewModel();
        public RegisterViewModel Register { get; set; } = new RegisterViewModel();
    }
}
