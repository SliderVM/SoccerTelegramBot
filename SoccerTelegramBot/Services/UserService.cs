using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace SoccerTelegramBot.Services
{
    public class UserService
    {
        private readonly DatabaseContext _databaseContext;        

        public UserService(DatabaseContext db) { 
            _databaseContext= db;
        }

        /// <summary>
        /// Возвращает пользователя из БД, если в БД нет, то создает и возвращает
        /// </summary>
        /// <param name="fromUser">Пользователь сделавший запрос</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task<Entities.User> GetUserAsync(User? fromUser, CancellationToken cancellationToken)
        {
            if (fromUser == null)
            {
                throw new ArgumentNullException(nameof(fromUser));
            }

            long userId = fromUser?.Id ?? 0;
            var user = await _databaseContext.Users.Where(x => x.Id == userId).Include(x => x.Signeds).Include(c => c.Subscriptions).FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (user == null)
            {
                var newUser = new Entities.User
                {
                    Id = userId,
                    UserName = fromUser?.Username,
                    LastName = fromUser?.LastName,
                    FirstName = fromUser?.FirstName,
                    IsAdmin = false
                };

                _databaseContext.Add(newUser);
                await _databaseContext.SaveChangesAsync(cancellationToken);

                user = newUser;
            }

            return user;
        }

        public async Task<Boolean> CheckAdmin(User? fromUser, CancellationToken cancellationToken)
        {
            var currentUser = await GetUserAsync(fromUser, cancellationToken);
            var suUserId = await _databaseContext.Configurations.Where(x => x.Label.Equals("su")).FirstOrDefaultAsync(cancellationToken);

            if (suUserId == null)
            {
                throw new Exception("В настройках не задан суперпользователь");
            }

            var suUser = await _databaseContext.Users.FindAsync(new object?[] { long.Parse(suUserId.Value) }, cancellationToken: cancellationToken);

            if (suUser is null || (!suUser.Equals(currentUser) && !currentUser.IsAdmin))
            {
                return false;
            }

            return true;
        }
    }
}
