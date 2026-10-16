import stat

import pytest
from cryptography.hazmat.primitives.serialization import load_pem_private_key, load_pem_public_key

from sso_deploy.key_pair import KEY_SIZE, generate, public_key_path


def test_public_key_path_sits_beside_the_private_key(tmp_path):
    assert public_key_path(tmp_path / "sso-signing.pem") == tmp_path / "sso-signing.pub.pem"


def test_generate_writes_a_private_key_only_its_owner_can_read(tmp_path):
    path = tmp_path / "key.pem"

    generate(path)

    key = load_pem_private_key(path.read_bytes(), password=None)
    assert key.key_size == KEY_SIZE
    assert stat.S_IMODE(path.stat().st_mode) == 0o600


def test_generate_writes_the_matching_public_key(tmp_path):
    path = tmp_path / "key.pem"

    generate(path)

    private_key = load_pem_private_key(path.read_bytes(), password=None)
    public_key = load_pem_public_key(public_key_path(path).read_bytes())
    assert public_key.public_numbers() == private_key.public_key().public_numbers()


def test_generate_refuses_to_replace_a_key(tmp_path):
    path = tmp_path / "key.pem"
    path.write_bytes(b"in use")

    with pytest.raises(FileExistsError):
        generate(path)

    assert path.read_bytes() == b"in use"
