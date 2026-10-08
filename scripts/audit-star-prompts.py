#!/usr/bin/env python3
"""Probe exported synthetic C# prompts through an external API.
Set LLM_BASE_URL, LLM_PAT and LLM_MODEL; export input with export-star-prompts.py.
Only synthetic replies and source digests are recorded, never credentials.
"""
import argparse
import concurrent.futures
import hashlib
import json
import os
from pathlib import Path
import urllib.request
import urllib.error

ROOT = Path(__file__).resolve().parents[1]

def probe(entry):
    result = {key:value for key,value in entry.items() if key not in ('messages', 'tools')}
    body = {'model':os.environ['LLM_MODEL'], 'messages':entry['messages'], 'tools':entry['tools'],
            'tool_choice':{'type':'function','function':{'name':'dm'}}, 'temperature':0,
            'max_tokens':600, 'thinking':{'type':'disabled'}}
    request = urllib.request.Request(os.environ['LLM_BASE_URL'].rstrip('/')+'/chat/completions',
        data=json.dumps(body).encode(), headers={'Authorization':'Bearer '+os.environ['LLM_PAT'], 'Content-Type':'application/json'})
    try:
        with urllib.request.build_opener(urllib.request.ProxyHandler({})).open(request,timeout=90) as response:
            data=json.load(response)
        replies=[{'tool':call['function']['name'],'arguments':json.loads(call['function']['arguments'])}
                 for call in data['choices'][0]['message'].get('tool_calls',[])]
        result.update(replies=replies, expectedIdReturned=len(replies)==1 and replies[0]['tool']=='dm'
            and replies[0]['arguments'].get('star_id')==entry['starId'])
    except urllib.error.HTTPError as error: result['error']='HTTP '+str(error.code)
    except Exception as error: result['error']=type(error).__name__
    return result

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--messages-file',required=True)
    parser.add_argument('--output',required=True)
    args=parser.parse_args()
    entries=json.loads(Path(args.messages_file).read_text());results=[]
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:
        futures=[executor.submit(probe,entry) for entry in entries]
        for index,future in enumerate(concurrent.futures.as_completed(futures),1):
            results.append(future.result())
            if index%10==0:print(f'Completed {index}/{len(entries)} synthetic probes',flush=True)
    output={'model':os.environ['LLM_MODEL'], 'syntheticOnly':True,
        'scope':'Actual C# DM prompt assembly and tool schemas; external API probes, not HTTP integration tests',
        'instructionsSha256':hashlib.sha256((ROOT/'backend/src/Reki.Application/Posts/StarAgentInstructions.cs').read_bytes()).hexdigest(),
        'catalogSha256':hashlib.sha256((ROOT/'frontend/src/api/personas.json').read_bytes()).hexdigest(),
        'results':sorted(results,key=lambda row:(row['world'],row['starId'],row['scenario']))}
    Path(args.output).write_text(json.dumps(output,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({'total':len(results),'providerErrors':sum('error' in row for row in results),
        'wrongOrMissingStarId':sum(not row.get('expectedIdReturned',False) for row in results if 'error' not in row)}),flush=True)
