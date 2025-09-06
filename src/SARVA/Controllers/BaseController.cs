using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SARVA.Controllers
{
    // Controller base para compartilhar funcionalidades, como obter o ID do usuário logado.
    public abstract class BaseController : Controller
    {
        // Propriedade para obter o ID do usuário logado a partir dos Claims.
        // Propriedade para obter o ID do usuário logado a partir dos Claims.
        protected readonly UserManager<IdentityUser> _userManager;

        public BaseController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // Propriedade para obter o ID do usuário logado de forma mais segura.
        protected string UsuarioIdLogado
        {
            get
            {
                // Usa o método GetUserId da classe UserManager, que é a forma recomendada.
                if (User != null)
                {
                    return _userManager.GetUserId(User);
                }
                return string.Empty;
            }
        }
    }
}