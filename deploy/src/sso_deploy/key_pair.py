"""generate-key-pair: writes an RSA key pair as PEM files, such as the one the SSO signs its tokens with."""

import argparse
import sys
from pathlib import Path

from cryptography.hazmat.primitives.asymmetric import rsa
from cryptography.hazmat.primitives.serialization import (
    Encoding,
    NoEncryption,
    PrivateFormat,
    PublicFormat,
)

from sso_deploy.certificate import owner_only

KEY_SIZE = 3072


def public_key_path(path: Path) -> Path:
    """Where the public key of the private key at path goes: key.pem's is key.pub.pem."""
    return path.with_suffix(".pub.pem")


def generate(path: Path) -> None:
    """Writes a new private key to path, readable only by its owner, and its public key beside it."""
    key = rsa.generate_private_key(public_exponent=65537, key_size=KEY_SIZE)
    private_pem = key.private_bytes(Encoding.PEM, PrivateFormat.PKCS8, NoEncryption())
    public_pem = key.public_key().public_bytes(Encoding.PEM, PublicFormat.SubjectPublicKeyInfo)
    with open(path, "xb", opener=owner_only) as file:
        file.write(private_pem)
    with open(public_key_path(path), "xb") as file:
        file.write(public_pem)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.partition(": ")[2])
    parser.add_argument("path", type=Path, help="where to write the private key, e.g. key.pem; it must not exist yet")
    args = parser.parse_args()
    for path in (args.path, public_key_path(args.path)):
        if path.exists():
            sys.exit(f"{path} already exists")

    generate(args.path)
    print(f"Wrote {args.path} and its public key {public_key_path(args.path)}. Keep the private key secret.")
