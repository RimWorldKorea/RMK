import os

MAX_INDEX = 100001
LIMIT_FILENAME = 170
WHITELISTED_PREFIX = ('ign_', 'Config')     # ('ign%', '이파일은수정하지말아줘' )
RKM_DATA_PATH = os.path.join(os.getcwd(), 'Data')


flag_array = 1 << MAX_INDEX
empty_room = 1


def set(idx: int, set_flag: bool = True):
    global flag_array
    if set_flag is True:
        flag_array |= 1 << idx
    else:
        flag_array &= ~(1 << idx)


def get(idx: int) -> bool:
    global flag_array
    return (flag_array & (1 << idx)) != 0


def get_empty_idx() -> int:
    global empty_room
    for i in range(empty_room, MAX_INDEX):
        if get(i) is False:
            empty_room = i
            return i
    return -1


if __name__ == '__main__':
    # 이미 사용 중인 파일 이름 탐색
    cnt_distinguish = 0
    for (root, dirs, files) in os.walk(RKM_DATA_PATH):
        for file_name in files:
            if '.xml' in str.lower(file_name) and file_name[:-4].isdecimal():
                idx = int(file_name[:-4])
                if get(idx) is True or idx >= MAX_INDEX:    # 파일명 중복 발생 시 or 인덱스를 초과할 경우
                    print(f'{os.path.join(root, file_name)}의 파일명이 중복되거나 인덱스를 초과하여, 임시로 temporary{cnt_distinguish}.xml로 변경합니다.')
                    os.rename(os.path.join(root, file_name), os.path.join(root, f'temporary{cnt_distinguish}.xml'))
                    cnt_distinguish += 1
                    continue
                set(idx, True)

    # 파일 이름을 바꿔야 하는 .xml 파일 탐색
    cnt_changed: int = 0
    for (root, dirs, files) in os.walk(RKM_DATA_PATH):
        for file_name in files:
            for prefix in WHITELISTED_PREFIX:
                if file_name.startswith(prefix):
                    break
            else:
                if '.xml' in str.lower(file_name) and not file_name[:-4].isdecimal():
                    src = os.path.join(root, file_name)
                    dest = os.path.join(root, '{0:05d}.xml'.format(int(get_empty_idx())))
                    print(f'{src} -> {os.path.basename(dest)}')
                    os.rename(src, dest)
                    set(get_empty_idx())
                    cnt_changed += 1

    # 지나치게 파일 경로가 긴 파일 탐색
    cnt_long_filename = 0
    for (root, dirs, files) in os.walk(RKM_DATA_PATH):
        for file_name in files:
            if '.xml' in str.lower(file_name):
                file_path = os.path.join(root, file_name)
                file_path = file_path[file_path.index('Data'):]
                if len(file_path) > LIMIT_FILENAME:
                    cnt_long_filename += 1
                    print(f'{LIMIT_FILENAME}자를 초과하는 파일 경로: {file_path}')

    if cnt_changed > 0:
        print(f'{cnt_changed}개의 파일들이 정상적으로 수정되었습니다.')
    elif cnt_changed == 0:
        print('수정할 파일이 없습니다.')
    
    if cnt_long_filename > 0:
        print(f'경고: {cnt_long_filename}개의 파일들이 {LIMIT_FILENAME}자를 초과하는 파일 경로를 가집니다.')
        exit(1)
