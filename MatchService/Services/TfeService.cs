using MatchService.Repositories;
using Microsoft.EntityFrameworkCore;
using TFELibrary.Data;
using TFELibrary.Shared;

namespace MatchService.Services
{
    public class TfeService : ITfeService
    {
        private readonly ITfeRepository _tfeRepository;
        private readonly ITagRepository _tagRepository;

        public TfeService(ITfeRepository repository, ITagRepository tagRepository)
        {
            _tfeRepository = repository;
            _tagRepository = tagRepository;
        }

        public async Task<TfeCreationResponse> CreateTfeAsync(TfeCreationRequest request, string authorId)
        {
            CheckTfe(request.Tfe);

            request.Tfe.Status = TFEStatus.Open;
            var tfe = CreateTfeEntity(request.Tfe, authorId);

            try
            {
                await MapTagsAndSkillsAsync(tfe, request.Tfe);

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

        public async Task<List<TfeDto>> GetTfesByAuthorIdAsync(string authorId)
        {
            if (string.IsNullOrWhiteSpace(authorId))
                throw new ArgumentException("El ID del autor no puede estar vacío.", nameof(authorId));

            var tfes = await _tfeRepository.GetByAuthorIdAsync(authorId);
            return tfes.Select(CreateTfeDto).ToList()!;
        }

        public async Task<bool> UpdateTfeAsync(int id, TfeUpdateRequest request, string authorId)
        {
            CheckTfe(request.Tfe);

            var existingTfe = await _tfeRepository.GetByIdAsync(id);

            if (existingTfe == null || existingTfe.AuthorId != authorId)
            {
                return false;
            }

            try
            {
                existingTfe.Title = request.Tfe.Title;
                existingTfe.Description = request.Tfe.Description;
                existingTfe.EstimatedDelivery = DateOnly.FromDateTime(request.Tfe.EstimatedDelivery);
                existingTfe.ExpirationDate = DateOnly.FromDateTime(request.Tfe.ExpirationDate);
                await MapTagsAndSkillsAsync(existingTfe, request.Tfe);
                await _tfeRepository.UpdateAsync(existingTfe);
                return true;
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException("Ocurrió un error al actualizar el TFE en la base de datos.", ex);
            }
        }

        public async Task<bool> DeleteTfeAsync(int id, string authorId)
        {
            if (string.IsNullOrWhiteSpace(authorId))
                throw new ArgumentException("Author ID cannot be empty.");

            return await _tfeRepository.DeleteAsync(id, authorId);
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
                Id = tfe.Id,
                Title = tfe.Title,
                Description = tfe.Description,
                TutorName = tfe.Author.FirstName + " " + tfe.Author.LastName,
                Topics = tfe.Topics.Select(tag => new TagDto { Name = tag.Name }).ToList(),
                RequiredSkills = tfe.RequiredSkills
                    .Where(skill => skill.Tag != null)
                    .Select(skill => new SkillDto { Tag = skill.Tag.Name, Level = skill.Level })
                    .ToList(),
                EstimatedDelivery = tfe.EstimatedDelivery.ToDateTime(TimeOnly.MinValue),
                ExpirationDate = tfe.ExpirationDate.ToDateTime(TimeOnly.MinValue),
                Status = tfe.Status,
            };
        }

        private TFE CreateTfeEntity(TfeDto dto, string authorId)
        {
            return new TFE
            {
                AuthorId = authorId,
                Title = dto.Title,
                Description = dto.Description,
                EstimatedDelivery = DateOnly.FromDateTime(dto.EstimatedDelivery),
                ExpirationDate = DateOnly.FromDateTime(dto.ExpirationDate),
                Status = dto.Status
            };
        }

        private async Task MapTagsAndSkillsAsync(TFE tfeEntity, TfeDto dto)
        {
            tfeEntity.Topics = new List<Tag>();
            tfeEntity.RequiredSkills = new List<TfeRequiredSkill>();

            if (dto.Topics != null)
            {
                foreach (var topicDto in dto.Topics)
                {
                    var tag = await _tagRepository.GetByNameAsync(topicDto.Name);
                    if (tag == null)
                        throw new ArgumentException($"Invalid '{topicDto.Name}' tag. Not found.");
                    tfeEntity.Topics.Add(tag);
                }
            }

            if (dto.RequiredSkills != null)
            {
                foreach (var skillDto in dto.RequiredSkills)
                {
                    var skillTag = await _tagRepository.GetByNameAsync(skillDto.Tag);
                    if (skillTag == null)
                        throw new ArgumentException($"Invalid '{skillDto.Tag}' tag. Not found.");

                    tfeEntity.RequiredSkills.Add(new TfeRequiredSkill
                    {
                        TagId = skillTag.Id,
                        Level = skillDto.Level
                    });
                }
            }
        }
    }
}