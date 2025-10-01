using Microsoft.AspNetCore.Identity;

namespace LearningWordsOnline.Services
{
    public class JapaneseIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DuplicateUserName(string email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = $"メールアドレス '{email}' は既に使われています。"
            };
        }

        public override IdentityError PasswordMismatch()
        {
            return new IdentityError
            {
                Code = nameof(PasswordMismatch),
                Description = "現在のパスワードが正しくありません。"
            };
        }

        public override IdentityError PasswordRequiresUpper()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresUpper),
                Description = "パスワードには最低1つの大文字（'A'-'Z'）が必要です。"
            };
        }

        public override IdentityError PasswordRequiresLower()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresLower),
                Description = "パスワードには最低1つの小文字（'a'-'z'）が必要です。"
            };
        }

        public override IdentityError PasswordRequiresDigit()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresDigit),
                Description = "パスワードには最低1つの数字（'0'-'9'）が必要です。"
            };
        }

        public override IdentityError PasswordRequiresNonAlphanumeric()
        {
            return new IdentityError
            {
                Code = nameof(PasswordRequiresNonAlphanumeric),
                Description = "パスワードには最低1つの記号が必要です。"
            };
        }

        public override IdentityError PasswordTooShort(int length)
        {
            return new IdentityError
            {
                Code = nameof(PasswordTooShort),
                Description = $"パスワードは{length}文字以上でなければなりません。"
            };
        }
    }
}
