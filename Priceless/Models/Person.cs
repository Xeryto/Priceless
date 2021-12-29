using System;
namespace Priceless.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public byte[] Image { get; set; }
    }
}
