using System;
namespace Priceless.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public int Grade { get; set; }
        public string Status { get; set; }
        public string FirstQA { get; set; }
        public string SecondQA { get; set; }
        public string ThirdQA { get; set; }
        public byte[] Image { get; set; }
    }
}
