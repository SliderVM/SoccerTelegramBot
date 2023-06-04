using Microsoft.EntityFrameworkCore;
using SoccerTelegramBot.Entities;

namespace SoccerTelegramBot.Services
{
    public class RulesService
    {
        const string RULES_LABEL = "rules";

        private readonly DatabaseContext _databaseContext;

        public RulesService(DatabaseContext databaseContext) { 
            _databaseContext = databaseContext;
        }

        public async Task<int> SetRulesAsync(string rulesText) {
            Configuration configuration = new()
            {
                Label = RULES_LABEL,
                Name = "Правила группы",
                Value = rulesText
            };

            _databaseContext.Add(configuration);
            return await _databaseContext.SaveChangesAsync();
        }

        public async Task<string> GetRules()
        {
            var rules = await _databaseContext.Configurations.Where(x => x.Label.Equals(RULES_LABEL)).FirstOrDefaultAsync();            
            return rules?.Value ?? "";
        }
    }
}
