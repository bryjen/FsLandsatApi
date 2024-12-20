module LandsatReflectance.Api.Utils.PasswordHashing

open Microsoft.AspNetCore.Identity

// TODO: Maybe find a better, 'more secure' form of hashing them passwords

let hashPassword (email: string) (password: string) =
    let passwordHasher = PasswordHasher<string>()
    passwordHasher.HashPassword(email, password)
    
let isHashValid (email: string) (password: string) (hashedPassword: string) =
    let passwordHasher = PasswordHasher<string>()
    match passwordHasher.VerifyHashedPassword(email, hashedPassword, password) with
    | PasswordVerificationResult.Success | PasswordVerificationResult.SuccessRehashNeeded -> true
    | _ -> false
