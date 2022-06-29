using TheBlogProject.Data;

namespace TheBlogProject.Services
{
    public class DataService
    {
        private readonly ApplicationDbContext _dbContext;

        public DataService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task ManageDataAsync()
        {
            // Task 1: Seed a few Roles into the system
            await SeedRolesAsync();

            // Task 2: Seed a few users into the system
            await SeedUsersAsync();
        }

        private async Task SeedRolesAsync()
        {
            // If there are already roles in the system, do nothing
            if (_dbContext.Roles.Any())
            {
                return;
            }

            // Otherwise, we want to create a few Roles


        }

        private async Task SeedUsersAsync()
        {

        }



    }
}
