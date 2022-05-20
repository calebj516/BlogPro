using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TheBlogProject.Models
{
    public class BlogUser : IdentityUser
    {
        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.", MinimumLength = 2)]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        [Required]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.", MinimumLength = 2)]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        public string DisplayName { get; set; }

        public byte[] ProfileImageData { get; set; }
        public string ContentType { get; set; } // records specific type of file used for image

        [StringLength(250, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.", MinimumLength = 2)]
        public string FacebookUrl { get; set; }
        [StringLength(250, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.", MinimumLength = 2)]
        public string TwitterUrl { get; set; }
        [StringLength(250, ErrorMessage = "The {0} must be at least {2} and no more than {1} characters long.", MinimumLength = 2)]
        public string GithubUrl { get; set; }

        [NotMapped]
        public string FullName
        {
            get
            {
                return $"{FirstName} {LastName}";
            }
        }

        public virtual ICollection<Blog> Blogs { get; set; } = new HashSet<Blog>();
        public virtual ICollection<Post> Posts { get; set; } = new HashSet<Post>();
        public virtual ICollection<Comment> Comments { get; set; } = new HashSet<Comment>();

    }
}
