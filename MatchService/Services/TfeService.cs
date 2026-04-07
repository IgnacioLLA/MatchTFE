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
                var tags = new List<Tag>();
                if (request.Tfe.Topics != null && request.Tfe.Topics.Count > 0)
                {
                    foreach (var topicDto in request.Tfe.Topics)
                    {
                        var tag = await _tagRepository.GetByNameAsync(topicDto.Name);
                        if (tag == null)
                            throw new ArgumentException($"El tag '{topicDto.Name}' no existe. Debe ser creado previamente.");
                        tags.Add(tag);
                    }
                    tfe.Topics = tags;
                }

                var requiredSkills = new List<TfeRequiredSkill>();
                if (request.Tfe.RequiredSkills != null && request.Tfe.RequiredSkills.Count > 0)
                {
                    foreach (var skillDto in request.Tfe.RequiredSkills)
                    {
                        var skillTag = await _tagRepository.GetByNameAsync(skillDto.Tag);
                        if (skillTag == null)
                            throw new ArgumentException($"El tag de habilidad '{skillDto.Tag}' no existe. Debe ser creado previamente.");

                        var requiredSkill = new TfeRequiredSkill
                        {
                            TagId = skillTag.Id,
                            Tag = skillTag,
                            Level = skillDto.Level
                        };
                        requiredSkills.Add(requiredSkill);
                    }
                }
                tfe.RequiredSkills = requiredSkills;

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
                RequiredSkills = tfe.RequiredSkills.Select(skill => new SkillDto { Tag = skill.Tag.Name, Level = skill.Level }).ToList(),
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
    }
}
