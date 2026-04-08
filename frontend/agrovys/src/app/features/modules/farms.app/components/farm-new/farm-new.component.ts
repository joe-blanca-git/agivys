import { CommonModule } from '@angular/common';
import {
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  ViewChild,
  OnInit,
} from '@angular/core';
import { FarmService } from '../../services/farm.service';
import { ToastService } from '../../../../../core/services/toast.service';
import { FarmPreviewMapComponent } from '../farm-preview-map/farm-preview-map.component';
import { LocalStorageUtils } from '../../../../../core/utils/localstorage';
import { loggUser } from '../../../../../shared/models/loggUser';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-farm-new',
  standalone: true,
  imports: [CommonModule, FarmPreviewMapComponent, FormsModule],
  templateUrl: './farm-new.component.html',
  styleUrl: './farm-new.component.scss',
})
export class FarmNewComponent implements OnInit {
  @ViewChild(FarmPreviewMapComponent) previewMap!: FarmPreviewMapComponent;
  @ViewChild('fileDropRef') fileDropRef!: ElementRef<HTMLInputElement>;
  @Output() loadingEmit = new EventEmitter<boolean>();

  localStorage = new LocalStorageUtils();

  isDragging: boolean = false;
  isLoading: boolean = true;
  isLoadingUpload: boolean = false;

  selectedFiles: File[] = [];
  selectedYear!: number;
  listYears: any[] = [];
  errorMessage: string = '';
  uploadedFiles: any;

  readonly allowedExtensions = ['.shp', '.shx', '.dbf', '.prj'];

  constructor(
    private farmService: FarmService,
    private toasService: ToastService,
  ) {}

  ngOnInit(): void {
    const currentYear = new Date().getFullYear();
    this.selectedYear = currentYear;
    this.listYears = [];
    for (let i = -5; i <= 5; i++) {
      this.listYears.push(currentYear + i);
    }

    this.loadData();
  }

  loadData() {
    setTimeout(() => {
      this.isLoading = true;
      this.loadingEmit.emit(true);
    }, 0);

    setTimeout(() => {
      this.isLoading = false;
      this.loadingEmit.emit(false);
    }, 500);
  }

  public submitNewFarm() {
    if (this.isLoadingUpload) return;

    if (this.errorMessage !== '' || this.selectedFiles.length < 4) {
      this.errorMessage =
        'Verifique os arquivos obrigatórios cadastrar a fazenda.';
      return;
    }

    this.isLoading = true;
    this.loadingEmit.emit(true);

    const user: loggUser = this.localStorage.getUser();
    const farmName = this.previewMap.shapeInfo.nome;
    const clientUnitId = user.companyId;
    const agivysUserId = user.id;
    const cropYear = '2026';

    const farmForm = {
      name: farmName,
      clientUnitId: clientUnitId,
      agivysUserId: agivysUserId,
      cropYear: cropYear,
    };

    this.farmService
      .createFarmWithBoundaries(farmForm, this.selectedFiles)
      .subscribe({
        next: (response) => {
          this.toasService.success('Fazenda Cadastrada com Sucesso!', 3000);
          this.isLoading = false;
          this.loadingEmit.emit(false);
        },
        error: (err) => {
          console.error('Erro ao cadastrar fazenda', err);
          this.isLoading = false;
          this.loadingEmit.emit(false);
        },
      });
  }

  public submitBoundary(): void {
    if (this.isLoadingUpload) return;

    if (this.errorMessage !== '' || this.selectedFiles.length < 4) {
      this.errorMessage = 'Verifique os arquivos obrigatórios antes de enviar.';
      return;
    }

    this.isLoadingUpload = true;
    this.loadingEmit.emit(true);

    const formData = new FormData();

    this.selectedFiles.forEach((file) => {
      formData.append('files', file);
    });

    this.farmService.uploadBoundary(formData).subscribe({
      next: (response) => {
        console.log('Dados do Shapefile lidos com sucesso:', response);

        this.uploadedFiles = response;
        this.isLoadingUpload = false;
        this.loadingEmit.emit(false);

        setTimeout(() => {
          if (this.previewMap && response.geojson) {
            this.previewMap.drawShapefile(
              response.geojson,
              response.area_total_ha,
            );
          }
        }, 150);

        this.toasService.success('Upload concluído com sucesso!', 5000);
      },
      error: (erro) => {
        this.isLoadingUpload = false;
        this.loadingEmit.emit(false);
      },
    });
  }

  // --- EVENTOS DE DRAG AND DROP ---
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragging = false;

    const files = event.dataTransfer?.files;
    if (files) {
      this.handleFiles(files);
    }
  }

  // --- EVENTO DE CLIQUE (SELEÇÃO NORMAL) ---
  onFileSelected(event: any): void {
    const files = event.target.files as FileList;
    if (files) {
      this.handleFiles(files);
    }

    if (this.fileDropRef) {
      this.fileDropRef.nativeElement.value = '';
    }
  }

  // --- LÓGICA DE VALIDAÇÃO E ARMAZENAMENTO ---
  private handleFiles(files: FileList): void {
    this.errorMessage = '';

    Array.from(files).forEach((file: File) => {
      const fileName = file.name.toLowerCase();

      const fileExtension = this.allowedExtensions.find((ext) =>
        fileName.endsWith(ext),
      );

      if (!fileExtension) {
        this.errorMessage = `Formato inválido detectado. Por favor, envie apenas arquivos ${this.allowedExtensions.join(', ')}.`;
        return;
      }

      const existingFileIndex = this.selectedFiles.findIndex((f) =>
        f.name.toLowerCase().endsWith(fileExtension),
      );

      if (existingFileIndex !== -1) {
        this.selectedFiles[existingFileIndex] = file;
      } else {
        this.selectedFiles.push(file);
      }
    });

    this.checkRequiredFiles();
  }

  public removeFile(index: number): void {
    this.selectedFiles.splice(index, 1);
    this.errorMessage = '';
    this.checkRequiredFiles();
  }

  private checkRequiredFiles(): void {
    if (this.selectedFiles.length === 0) {
      this.errorMessage = '';
      return;
    }

    const uploadedExts = this.selectedFiles.map((f) => {
      const parts = f.name.toLowerCase().split('.');
      return `.${parts[parts.length - 1]}`;
    });

    const hasShp = uploadedExts.includes('.shp');
    const hasShx = uploadedExts.includes('.shx');
    const hasDbf = uploadedExts.includes('.dbf');
    const hasPrj = uploadedExts.includes('.prj');

    if (!hasShp || !hasShx || !hasDbf || !hasPrj) {
      this.errorMessage =
        'Atenção: Para o talhão ser carregado corretamente, os arquivos .shp, .shx, .dbf e .prj precisam ser enviados juntos.';
    } else {
      this.errorMessage = '';

      // AUTO-SUBMIT: Validação passou, chama a API automaticamente!
      this.submitBoundary();
    }
  }

  public formatBytes(bytes: number, decimals = 2): string {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
  }

  getStatusForm() {
    return !this.isLoading;
  }
}
