"""Publish approved public copy using the deployment's bot credential."""
import json
import os
from pathlib import Path
import urllib.request


def main():
    text = Path('docs/positioning.md').read_text(encoding='utf-8')
    description = text.split('## Описание игрового бота\n\n', 1)[1].split('\n\nКороткое описание:', 1)[0].strip()
    short = text.split('Короткое описание: ', 1)[1].split('\n', 1)[0].strip()
    if len(description) > 512 or len(short) > 120:
        raise RuntimeError('Description exceeds Telegram limits')
    credential = os.environ['TG_PAT']

    def call(method, data=None):
        req = urllib.request.Request(
            'https://api.telegram.org/bot' + credential + '/' + method,
            data=json.dumps(data or {}).encode(), headers={'Content-Type': 'application/json'},
        )
        try:
            with urllib.request.urlopen(req, timeout=30) as response:
                result = json.load(response)
        except Exception:
            raise RuntimeError('Telegram request failed; credential and request URL withheld') from None
        if not result.get('ok'):
            raise RuntimeError('Telegram rejected description update')
        return result['result']

    identity = call('getMe')
    if identity['username'] != 'reki_appbot':
        raise RuntimeError('Credential does not belong to the game bot')
    previous = {}
    for language in ('', 'ru'):
        previous[language] = {
            'description': call('getMyDescription', {'language_code': language})['description'],
            'short_description': call('getMyShortDescription', {'language_code': language})['short_description'],
        }
    Path('bot-description-before.json').write_text(json.dumps(previous, ensure_ascii=False, indent=2), encoding='utf-8')
    for language in ('', 'ru'):
        call('setMyDescription', {'description': description, 'language_code': language})
        call('setMyShortDescription', {'short_description': short, 'language_code': language})
        if call('getMyDescription', {'language_code': language})['description'] != description:
            raise RuntimeError('Description verification failed')
        if call('getMyShortDescription', {'language_code': language})['short_description'] != short:
            raise RuntimeError('Short description verification failed')
    before = call('getUserProfilePhotos', {'user_id': identity['id'], 'limit': 1})
    boundary = 'RekiBotAvatarUpload'
    photo = Path('docs/design/assets/reki-bot-avatar.jpg').read_bytes()
    body = (
        f'--{boundary}\r\nContent-Disposition: form-data; name="photo"\r\n\r\n'.encode()
        + json.dumps({'type': 'static', 'photo': 'attach://avatar'}).encode()
        + f'\r\n--{boundary}\r\nContent-Disposition: form-data; name="avatar"; filename="avatar.jpg"\r\nContent-Type: image/jpeg\r\n\r\n'.encode()
        + photo + f'\r\n--{boundary}--\r\n'.encode()
    )
    request = urllib.request.Request(
        'https://api.telegram.org/bot' + credential + '/setMyProfilePhoto',
        data=body, headers={'Content-Type': 'multipart/form-data; boundary=' + boundary},
    )
    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            updated = json.load(response)
    except Exception:
        raise RuntimeError('Avatar update failed; credential and URL withheld') from None
    if not updated.get('ok') or updated.get('result') is not True:
        raise RuntimeError('Telegram rejected avatar update')
    after = call('getUserProfilePhotos', {'user_id': identity['id'], 'limit': 1})
    if not after['photos'] or after['photos'] == before['photos']:
        raise RuntimeError('New avatar verification failed')
    file = call('getFile', {'file_id': after['photos'][0][-1]['file_id']})
    try:
        with urllib.request.urlopen('https://api.telegram.org/file/bot' + credential + '/' + file['file_path'], timeout=30) as response:
            Path('bot-avatar-after.jpg').write_bytes(response.read())
    except Exception:
        raise RuntimeError('Avatar verification download failed; URL withheld') from None
    print('Game bot avatar and descriptions verified (default and Russian)')


if __name__ == '__main__':
    main()
