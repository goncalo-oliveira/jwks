# JWKS

A command-line tool to manage a local JSON Web Key Set (JWKS) for signing and verifying JSON Web Tokens (JWTs) on development environments.

## Concepts

- **JWKS**: A JSON Web Key Set is a JSON structure that represents a set of cryptographic keys. It is used to sign and verify JWTs.
- **JWT**: A JSON Web Token is a compact, URL-safe means of representing claims to be transferred between two parties. It is commonly used for authentication and authorization.

A local JWKS contains a public/private key pair used to sign and verify JWTs during development. The private key is used to sign tokens, while the public key is offered as a JSON file that can be consumed by applications to verify the tokens.

The command-line tool can either use a global JWKS stored in the user's home directory or a project-specific JWKS stored in a `.jwks` folder within the project directory.

## Getting Started

To be able to issue and verify tokens, the JWKS must first be initialized. This can be done using the `init` command:

```bash
jwks init
```

This command creates a new JWKS with a signing key pair. By default, it creates the JWKS in a `.jwks` folder within the user's home directory. To indicate an explicit path (e.g., project-specific), use it as an argument:

```bash
jwks init .
```

This creates the JWKS in a `.jwks` folder within the current directory.

To verify that the JWKS was created successfully, you can use the `status` command:

```bash
jwks status
```

## Order of Precedence

When using the JWKS tool, it determines which JWKS to use based on a certain order of precedence, if no explicit path is provided:

1. **Current Directory**: The tool checks for a `.jwks` folder in the current working directory and uses it if found.
2. **Global JWKS**: The tool checks for a `.jwks` folder in the user's home directory and uses it if found.

Most commands (except `init`) support the `--jwks-path` option to explicitly specify the path to the JWKS, ignoring the order of precedence.

## Generating Keys

Before issuing tokens, ensure that the JWKS contains a signing key pair. To generate a new key pair, use the `keygen` command:

```bash
jwks keygen
```

Keys can also be generated *ad-hoc*, without storing them in the JWKS, by using the `--export` option. This outputs the generated key pair directly to the console or, if combined with the `--out` option, to a specified path:

```bash
jwks keygen --export --out .
```

## Issuing Tokens

To issue a new JWT using the JWKS, use the `token` command:

```bash
jwks token --aud your-audience --sub your-subject --claim key1=value1 --claim key2=value2
```

> [!TIP]
> The `--aud` and `--sub` options are equivalent to `--claim aud=...` and `--claim sub=...`, respectively.
