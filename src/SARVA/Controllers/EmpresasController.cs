using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SARVA.Data;
using SARVA.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SARVA.Controllers
{
    [Authorize]
    public class EmpresasController : BaseController
    {
        private readonly SARVADbContext _db;

        public EmpresasController(SARVADbContext db, UserManager<IdentityUser> userManager) : base(userManager)
        {
            _db = db;
        }

        public IActionResult BuscarEmpresa(int requisitando, string nomeCliente)
        {
            // 1 -> AdicionarProdutoRequisitado, 2 -> AdicionarVenda, etc.
            ViewBag.Requisitando = requisitando;
            ViewBag.NomeCliente = nomeCliente;
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult BuscarEmpresa(string nomeCliente, int requisitando)
        {
            return RedirectToAction("BuscarEmpresa", "Empresas", new { requisitando = requisitando, nomeCliente = nomeCliente});
        }

        [HttpPost]
        public async Task<IActionResult> GetSearchValue(string search)
        {
            var empresas = await _db.Empresas
                .Where(x => x.razao_social.Contains(search) && x.flag == true)
                .Select(x => new {
                    razaoSocial = x.razao_social,
                    id_Usuario = x.id_usuario,
                    flag = x.flag
                }).ToListAsync();

            return Json(empresas);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LerDadosEmpresa()
        {
            var listOfData = await _db.Empresas.ToListAsync();
            return View(listOfData);
        }

        [HttpGet]
        public IActionResult AdicionarEmpresa()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarEmpresa(Empresa model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (await _db.Empresas.AnyAsync(x => x.id == model.id))
            {
                ViewBag.JaExiste = "Essa empresa já foi requisitada";
                return View(model);
            }

            if (User.IsInRole("Admin"))
            {
                model.id_usuario = Convert.ToString(UsuarioIdLogado); // Associa ao usuário que está criando
                model.flag = true;
                _db.Empresas.Add(model);
                await _db.SaveChangesAsync();
                ViewBag.Message = "Data Insert Successfully";
                return RedirectToAction("LerDadosEmpresa", "Empresas");
            }
            else
            {
                model.id_usuario = Convert.ToString(UsuarioIdLogado); // Associa ao usuário que está criando
                model.flag = false;
                _db.Empresas.Add(model);
                await _db.SaveChangesAsync();
                ViewBag.Message = "Data Insert Successfully";
                return RedirectToAction("Index", "Home");
            }



        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditarEmpresa(int id)
        {
            var data = await _db.Empresas.FirstOrDefaultAsync(x => x.id == id);
            if (data == null) return NotFound();
            return View(data);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEmpresa(Empresa model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var data = await _db.Empresas.FirstOrDefaultAsync(x => x.id == model.id);
            if (data != null)
            {
                data.id_usuario = model.id_usuario;
                data.razao_social = model.razao_social;
                data.flag = model.flag;
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(LerDadosEmpresa));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverEmpresa(int id)
        {
            var data = await _db.Empresas.FirstOrDefaultAsync(x => x.id == id);
            if (data != null)
            {
                _db.Empresas.Remove(data);
                await _db.SaveChangesAsync();
                ViewBag.Message = "Record Delete Successfully";
            }
            return RedirectToAction("LerDadosEmpresa", "Empresas");
        }
    }
}