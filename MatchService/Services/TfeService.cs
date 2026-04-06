using MatchService.Repositories;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Services
{
    public class TfeService : ITfeService
    {
        private readonly ITfeRepository _tfeRepository;

        public TfeService(ITfeRepository repository)
        {
            _tfeRepository = repository;
        }

        public async Task<TfeCreationResponse> CreateTfeAsync(TfeCreationRequest request, string authorId)
        {
            CheckTfe(request.Tfe);

            var tfe = CreateTfeEntity(request.Tfe, authorId);
            try
            {
                var createdTfe = await _tfeRepository.CreateAsync(tfe);
                var dto = CreateTfeDto(createdTfe);

                return new TfeCreationResponse { Tfe = dto, TfeId = createdTfe.Id };
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Ocurrió un error al guardar el TFE en la base de datos.", ex);
            }
        }

        public async Task<TfeDto?> GetTfeByIdAsync(int id)
        {
            return CreateTfeDto(await _tfeRepository.GetByIdAsync(id));

        }

        // -------------------------------------------------------
        private void CheckTfe(TfeDto tfe)
        {
            if (tfe == null) throw new ArgumentNullException(nameof(tfe));
            if (string.IsNullOrWhiteSpace(tfe.Title)) throw new ArgumentException("El título es obligatorio.", nameof(tfe));
            if (string.IsNullOrWhiteSpace(tfe.Description)) throw new ArgumentException("La descripción es obligatoria.", nameof(tfe));
        }

        private TfeDto? CreateTfeDto(TFE? tfe)
        {
            if (tfe == null) return null;
            return new TfeDto
            {
                Title = tfe.Title,
                Description = tfe.Description,
                TutorName = tfe.Author.FirstName + " " + tfe.Author.LastName,
                Topics = tfe.Topics.Select(tag => new TagDto { Name = tag.Name }).ToList(),
                EstimatedDelivery = tfe.EstimatedDelivery.ToDateTime(TimeOnly.MinValue),
            };
        }

        private TFE CreateTfeEntity(TfeDto dto, string authorId)
        {
            return new TFE
            {
                AuthorId = authorId,
                Title = dto.Title,
                Description = dto.Description,
                EstimatedDelivery = DateOnly.FromDateTime(dto.EstimatedDelivery)
            };
        }
    }
}
