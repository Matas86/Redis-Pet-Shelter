using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using StackExchange.Redis;
using Extentions;

namespace RedisProj
{

    class RedisApp
    {
        IDatabase db;

        ConnectionMultiplexer redis;

        string LoggedUser;
        enum Commands
        {
            LOGIN,
            REGISTER,
            LOGOUT,
            PET_LIST,
            ADD_PET,
            TAKE_PET,
            AVAILABLE_PET,
            END
        }

        public static T ConvertFromRedis<T>(HashEntry[] hashEntries)
        {
            PropertyInfo[] properties = typeof(T).GetProperties();
            var obj = Activator.CreateInstance(typeof(T));
            foreach (var property in properties)
            {
                HashEntry entry = hashEntries.FirstOrDefault(g => g.Name.ToString().Equals(property.Name));
                if (entry.Equals(new HashEntry())) continue;
                property.SetValue(obj, Convert.ChangeType(entry.Value.ToString(), property.PropertyType));
            }
            return (T)obj;
        }
        public void Connect()
        {
            try
            {
                redis = ConnectionMultiplexer.Connect("localhost:6379");
                db = redis.GetDatabase();

                Console.WriteLine("Connection to RedisDB localhost:6379 was successful.");
                if (db.StringGet("usercount") == RedisValue.Null) db.StringSet("usercount", 0);
                if (db.StringGet("petcount") == RedisValue.Null) db.StringSet("petcount", 0);
                if (db.StringGet("sheltercount") == RedisValue.Null) db.StringSet("sheltercount", 0);
                Start();
            }
            catch (RedisConnectionException ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        public string getMD5Pass(string password)
        {
            var md5 = new MD5CryptoServiceProvider();
            byte[] inBytes = Encoding.UTF8.GetBytes(password);
            byte[] OutBytes = md5.ComputeHash(inBytes);
            StringBuilder output = new StringBuilder();
            foreach (var item in OutBytes)
            {
                output.Append(item.ToString("X2"));
            }
            return output.ToString();
        }

        public void Login()
        {
            if (LoggedUser == null)
            {
                Console.WriteLine("Enter your username.");
                string user = Console.ReadLine();
                Console.WriteLine("Enter your password.");
                string password = Console.ReadLine();

                password = getMD5Pass(password);

                var tran = db.CreateTransaction();
                var id = db.StringGet(user);
                tran.AddCondition(Condition.HashEqual("users:" + id, "password", password));
                bool committed = tran.Execute();
                if (committed) Console.WriteLine("You were logged in as " + user + ".");
                else Console.WriteLine("Incorrect password");
                LoggedUser = "users:" + user;
            }
            else Console.WriteLine("You are already logged in as " + LoggedUser + ".");
        }

        public void Register()
        {
            Console.WriteLine("Enter your username.");
            string user = Console.ReadLine();
            Console.WriteLine("Enter your password.");
            string password = Console.ReadLine();
            password = getMD5Pass(password);


            db.StringIncrement("usercount");
            string count = db.StringGet("usercount");
            var tran = db.CreateTransaction();
            tran.AddCondition(Condition.HashNotExists("users:" + count, "username"));
            HashEntry[] userFormat = { new HashEntry("username", user), new HashEntry("password", password) };
            tran.HashSetAsync("users:" + count, userFormat);
            tran.StringSetAsync(user, count);
            bool committed = tran.Execute();
            if (committed) Console.WriteLine("User creation was successful.");
            else Console.WriteLine("User already exists.");
            LoggedUser = "users:" + user;

        }

        public void Logout()
        {
            if (LoggedUser == null) Console.WriteLine("You are currently not logged in.");
            else
            {
                LoggedUser = null;
                Console.WriteLine("Logging off was successful.");
            }
        }

        public void SeePets()
        {
            string count = db.StringGet("petcount");

            for (int i = 1; i <= int.Parse(count); i++)
            {
                Console.WriteLine(i + ".");
                var pet = db.HashGetAll("pets:" + i);
                foreach (var item in pet)
                {
                    Console.WriteLine(string.Format("{0} - {1}", item.Name, item.Value));
                }
            } 
        }

        public void AddPet()
        {
            Console.WriteLine("Enter your pets name.");
            string petname = Console.ReadLine();
            Console.WriteLine("Enter your pets type.");
            string petstype = Console.ReadLine();
            Console.WriteLine("Is your pet available for pick-up? (Yes/No).");
            string petsavailb = Console.ReadLine();
            Console.WriteLine("In what shelter are you placing your pet?");
            string petShelter = Console.ReadLine();


            db.StringIncrement("petcount");
            string count = db.StringGet("petcount");
            var tran = db.CreateTransaction();
            tran.AddCondition(Condition.HashNotExists("pets:" + count, "petname"));
            HashEntry[] petFormat = { new HashEntry("petname", petname), new HashEntry("petstype", petstype), new HashEntry("petsAvailability", petsavailb), new HashEntry("Shelter", petShelter )};
            tran.HashSetAsync("pets:" + count, petFormat);
            tran.StringSetAsync(petname, count);
            bool committed = tran.Execute();
            if (committed) Console.WriteLine("Pet was added successfully.");
            else Console.WriteLine("Pet was already added previously.");
        }

        public void SeeAvailbPets()
        {
            string count = db.StringGet("petcount");

            for (int i = 1; i <= int.Parse(count); i++)
            {
                
                var pet = db.HashGetAll("pets:" + i);
                HashEntry entry = new HashEntry("petsAvailability", "Yes");

                foreach (var item in pet)
                {
                    if (item == entry)
                    {
                        foreach (var items in pet)
                            Console.WriteLine(string.Format("{0} - {1}", items.Name, items.Value));
                        Console.WriteLine("\n");
                    }
                    
                }
            }
        }

        public void TakePet()
        {
            Console.WriteLine("Enter your pets name.");
            string petname = Console.ReadLine();
            Console.WriteLine("Enter your pets type.");
            string petstype = Console.ReadLine();
            Console.WriteLine("In what shelter is the pet located right now?");
            string petShelter = Console.ReadLine();

            var id = db.StringGet(petname);
            var tran = db.CreateTransaction();
            tran.AddCondition(Condition.HashEqual("pets:" + id, "Shelter", petShelter));
            HashEntry[] petFormat = { new HashEntry("petname", petname), new HashEntry("petstype", petstype), new HashEntry("petsAvailability", "No"), new HashEntry("Shelter", "New Owner: "+LoggedUser) };
            tran.HashSetAsync("pets:" + id, petFormat);
            bool committed = tran.Execute();
            if (committed) Console.WriteLine("Pet was taken successfully.");
            else Console.WriteLine("Something went wrong.");


        }

        public void ListShelters()
        {
            string count = db.StringGet("sheltercount");

            for (int i = 1; i <= int.Parse(count); i++)
            {
                Console.WriteLine(i + ".");
                var pet = db.HashGetAll("shelters:" + i);
                foreach (var item in pet)
                {
                    Console.WriteLine(string.Format("{0} - {1}", item.Name, item.Value));
                }
            }
        }

        public void AddShelter()
        {
            Console.WriteLine("Enter your shelters name.");
            string sheltername = Console.ReadLine();

            db.StringIncrement("sheltercount");
            string count = db.StringGet("sheltercount");
            var tran = db.CreateTransaction();
            tran.AddCondition(Condition.HashNotExists("shelters:" + count, "sheltername"));
            HashEntry[] shelterFormat = { new HashEntry("sheltername", sheltername) };
            tran.HashSetAsync("shelters:" + count, shelterFormat);
            bool committed = tran.Execute();
            if (committed) Console.WriteLine("Shelter was added successfully.");
            else Console.WriteLine("This shelter was already added previously.");
        }


        public void WriteMenu()
        {

            Console.WriteLine("\nSelect one of the options. by typing its number (without the dot)");
            Console.WriteLine("1. Login");
            Console.WriteLine("2. Register");
            Console.WriteLine("3. Logout");
            Console.WriteLine("4. See all the pets");
            Console.WriteLine("5. Add your pet");
            Console.WriteLine("6. See all the available pets");
            Console.WriteLine("7. Take pet");
            Console.WriteLine("8. Add pet shelter");
            Console.WriteLine("9. List all pet shelters");
            Console.WriteLine("10. End sesion\n");
        }
        public void Start()
        {

            while (true)
            {
                WriteMenu();
                int option = int.Parse(Console.ReadLine());
                switch (option)
                {
                    case 1:
                        {
                            Login();
                            break;
                        }
                    case 2:
                        {
                            Register();
                            break;
                        }
                    case 3:
                        {
                            Logout();
                            break;

                        }
                    case 4:
                        {
                            SeePets();
                            break;
                        }
                    case 5:
                        {
                            AddPet();
                            break;

                        }
                    case 6:
                        {
                            SeeAvailbPets();
                            break;

                        }
                    case 7:
                        {
                            TakePet();
                            break;

                        }
                    case 8:
                        {
                            AddShelter();
                            break;

                        }
                    case 9:
                        {
                            ListShelters();
                            break;

                        }
                    case 10:
                        {
                            return;
                        }

                }
            }
        }

    }
}
