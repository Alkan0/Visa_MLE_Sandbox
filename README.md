# Visa MLE Helper API

A lightweight ASP.NET Core API used to **encrypt and decrypt Visa Message Level Encryption (MLE) payloads** during development and testing.

This tool helps when integrating with **Visa APIs that require MLE**, such as the Alias Directory Service, by providing endpoints that:

- Encrypt request payloads before sending them to Visa
- Decrypt encrypted responses returned by Visa

---

# Endpoints

## Encrypt payload

Encrypts a JSON payload using the **Visa server public certificate** and returns the `encData` object required by Visa APIs.

## POST /api/mle/encrypt
### Request body

Send the **plain JSON payload** you want to send to Visa.

Example:

```json
{
  "aliasValue": "306900000001",
  "aliasType": "PHONE",
  "profile": {
    "firstName": "Alex",
    "lastName": "Miller"
  }
}

