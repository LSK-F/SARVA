using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SARVA.Data;
using SARVA.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SARVA.Controllers
{
    [Authorize(Roles = "Admin")] // Apenas Admins podem acessar este controller
    public class CiclosController : BaseController
    {
        private readonly SARVADbContext _db;

        public CiclosController(SARVADbContext db, UserManager<IdentityUser> userManager) : base(userManager)
        {
            _db = db;
        }

        public async Task<IActionResult> LerDadosCiclo()
        {
            var listOfData = await _db.Ciclos
                                      .Include(c => c.Empresa)
                                      .OrderBy(x => x.Empresa.razao_social)
                                      .ToListAsync();
            return View(listOfData);
        }

        [HttpGet]
        public IActionResult AdicionarCiclo()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarCiclo(Ciclo model, string razaoSocial)
        {
            //var razaoSocial = Convert.ToString(Request["razaoSocial"]);
            var empresa = await _db.Empresas.FirstOrDefaultAsync(x => x.razao_social == razaoSocial);
            if (empresa == null)
            {
                ModelState.AddModelError("", "Empresa não encontrada");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                model.id_usuario = Convert.ToString(UsuarioIdLogado);
                model.id_empresa = empresa.id;
                _db.Ciclos.Add(model);
                await _db.SaveChangesAsync();
                return RedirectToAction("LerDadosCiclo", "Ciclos");
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditarCiclo(int id)
        {
            var ciclo = await _db.Ciclos.FirstOrDefaultAsync(x => x.id == id);
            if (ciclo == null) return NotFound();
            return View(ciclo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCiclo(Ciclo model, DateTime dataInicio, DateTime dataFim)
        {
            if (!ModelState.IsValid) return View(model);

            var ciclo = await _db.Ciclos.FirstOrDefaultAsync(x => x.id == model.id);
            if (ciclo == null) return NotFound();

            ciclo.nome = model.nome;
            ciclo.dataInicio = dataInicio;
            ciclo.dataFim = dataFim;
            await _db.SaveChangesAsync();

            return RedirectToAction("LerDadosCiclo", "Ciclos");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverCiclo(int id)
        {
            var ciclo = await _db.Ciclos
                               .Include(c => c.Produtos)
                               .FirstOrDefaultAsync(x => x.id == id);
            if (ciclo == null) return NotFound();

            if (ciclo.Produtos.Any())
            {
                TempData["ErrorMessage"] = $"Não é possível remover o ciclo '{ciclo.nome}', pois ele possui produtos associados.";
            }
            else
            {
                _db.Ciclos.Remove(ciclo);
                await _db.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Ciclo '{ciclo.nome}' removido com sucesso.";
            }


            return RedirectToAction("LerDadosCiclo", "Ciclos");
        }
    }
}