using SoccerTelegramBot.Data;

namespace SoccerTelegramBot.Services
{
    public class StepService
    {
        private List<StepModel> _steps = new(); 

        public StepService() { } 

        public void AddStep(StepModel step) { _steps.Add(step); }
        public void RemoveStep(StepModel step) { _steps.Remove(step); }
        public StepModel? GetStep(long userId) { 
            var step = _steps.FirstOrDefault(x => x.UserId == userId);
            return step; 
        }
    }
}
