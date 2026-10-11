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

    if call('getMe')['username'] != 'reki_appbot':
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
    print('Game bot description verified (default and Russian)')


if __name__ == '__main__':
    main()
