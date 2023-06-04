using Microsoft.EntityFrameworkCore;
using SoccerTelegramBot.Entities;

namespace SoccerTelegramBot.Services
{
    public class GameDay
    {
        private DatabaseContext _databaseContext;

        public GameDay(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }   

        public async Task<string> GetDayAsync()
        {
            var gameTime = await GetGameTimeAsync();
            var dateGame = await GetDateGameAsync();
            return dateGame.Date.ToString("D") + gameTime.ToString();
        }

        public async Task<DateTime> GetDateGameAsync()
        {
            var gameDay = await _databaseContext.Configurations.Where(x => x.Label.Equals("gameday")).FirstOrDefaultAsync();

            if (gameDay is null)
            {
                throw new Exception("В конфигурации отсутсвует день игры");
            }

            var now = DateTime.Now.Date;
            var nowDayOfWeek = (int)now.DayOfWeek;
            var dayOfWeekGame = int.Parse(gameDay.Value);

            var offset = (7 - nowDayOfWeek + dayOfWeekGame) % 7;

            return now.AddDays(offset);

        }

        public async Task<TimeOnly?> GetGameTimeAsync()
        {
            var gameTime = await _databaseContext.Configurations.Where(x => x.Label.Equals("gametime")).FirstOrDefaultAsync();
            return TimeOnly.Parse(gameTime.Value);
        }
    }
}
