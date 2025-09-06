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
    [Authorize] // Garante que apenas usuários logados possam acessar este controller
    public class ClientesController : BaseController
    {
        private readonly SARVADbContext _db;

        // Injeção de Dependência do DbContext
        public ClientesController(SARVADbContext db, UserManager<IdentityUser> userManager) : base(userManager)
        {
            _db = db;
        }

        // O atributo [Authorize] garante que apenas usuários logados possam acessar esta página.
        // Ele substitui a verificação 'if (!User.Identity.IsAuthenticated)'.
        [Authorize]
        public IActionResult BuscarCliente()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetSearchValue(string search)
        {
            var clientes = await _db.Clientes
                .Where(x => x.nome.Contains(search) && x.id_usuario == Convert.ToString(UsuarioIdLogado))
                .Select(x => new { nomeCliente = x.nome, id_Usuario = x.id_usuario })
                .ToListAsync();

            return Json(clientes);
        }

        public async Task<IActionResult> LerDadosCliente(string nomeCliente, DateTime? filtroNiver)
        {
            var idUsuario = Convert.ToString(UsuarioIdLogado);

            // A consulta base é mais eficiente, filtrando no banco de dados
            var query = _db.Clientes
                           .Include(c => c.Venda) // Carrega as vendas relacionadas para evitar N+1 queries
                           .Where(x => x.id_usuario == idUsuario);

            if (!string.IsNullOrEmpty(nomeCliente))
            {
                query = query.Where(x => x.nome == nomeCliente);
            }
            else if (filtroNiver.HasValue)
            {
                query = query.Where(x => x.aniversario == filtroNiver.Value);
            }

            var listOfData = await query.OrderBy(x => x.nome).ToListAsync();

            // Lógica de cálculo de score (pode ser movida para uma classe de serviço)
            foreach (var cliente in listOfData)
            {
                if (cliente.Venda == null || !cliente.Venda.Any())
                {
                    cliente.scoreId = 3; // Neutro
                    continue;
                }

                int totalVendas = cliente.Venda.Count;
                int vendasPagasNoPrazo = cliente.Venda
                    .Count(v => v.data_pagamento.HasValue && v.data_pagamento.Value <= v.data_vencimento.AddDays(7));

                double proporcaoPagas = (double)vendasPagasNoPrazo / totalVendas;

                if (proporcaoPagas >= 0.6)
                {
                    cliente.scoreId = 1; // Bom Pagador
                }
                else if (proporcaoPagas < 0.4)
                {
                    cliente.scoreId = 2; // Mau Pagador
                }
                else
                {
                    cliente.scoreId = 3; // Neutro
                }
            }

            await _db.SaveChangesAsync();
            return View(listOfData);
        }

        [HttpGet]
        public IActionResult AdicionarCliente()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdicionarCliente(Cliente model)
        {
            if (ModelState.IsValid)
            {
                model.id_usuario = Convert.ToString(UsuarioIdLogado); // Associa o cliente ao usuário logado
                model.scoreId = 3;
                _db.Clientes.Add(model);
                await _db.SaveChangesAsync();
                ViewBag.Message = "Data Insert Successfully";
                return RedirectToAction(nameof(LerDadosCliente));
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditarCliente(int id)
        {
            var data = await _db.Clientes.FirstOrDefaultAsync(x => x.id == id && x.id_usuario == Convert.ToString(UsuarioIdLogado));
            if (data == null)
            {
                return NotFound();
            }
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarCliente(Cliente model)
        {
            if (ModelState.IsValid)
            {
                var data = await _db.Clientes.Include(c => c.score).FirstOrDefaultAsync(x => x.id == model.id);
                if (data != null && data.id_usuario == Convert.ToString(UsuarioIdLogado))
                {
                    data.nome = model.nome;
                    data.email = model.email;
                    data.aniversario = model.aniversario;
                    data.scoreId = model.scoreId;
                    await _db.SaveChangesAsync();
                    return RedirectToAction("LerDadosCliente", "Clientes");
                }
                return NotFound();
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoverCliente(int id)
        {
            var cliente = await _db.Clientes
                .Include(c => c.Venda)
                .ThenInclude(v => v.Item_Venda)
                .FirstOrDefaultAsync(x => x.id == id && x.id_usuario == Convert.ToString(UsuarioIdLogado));

            if (cliente == null)
            {
                return NotFound();
            }

            // Remove entidades relacionadas em cascata
            foreach (var venda in cliente.Venda.ToList())
            {
                _db.Item_Venda.RemoveRange(venda.Item_Venda);
                _db.Venda.Remove(venda);
            }

            _db.Clientes.Remove(cliente);
            await _db.SaveChangesAsync();

            ViewBag.Message = "Record Delete Successfully";
            return RedirectToAction(nameof(LerDadosCliente));
        }
    }
}